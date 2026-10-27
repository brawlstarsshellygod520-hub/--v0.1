using System;
using System.Collections.Generic;
using UnityEngine;

namespace FireRescue
{
    /// <summary>
    /// A resident that leaves automatically when the fire grid
    /// contains a complete safe route to the assigned exit.  This deliberately owns no
    /// player or network state: a future authority can call <see cref="Tick"/> directly.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class RescueNpc : MonoBehaviour
    {
        public enum State
        {
            WaitingForSafeRoute = 1,
            Escaping,
            Rescued
        }

        private const int FixedObstacleMask = 1 << 8;
        private const float RouteRefreshInterval = 0.25f;
        // Keep the graph's clearance slightly wider than the controller.  A route that
        // only fits a point through a corner is not a route a CharacterController can use.
        private const float ClearancePadding = 0.035f;
        private const float StuckRepathDelay = 0.35f;
        private static readonly Vector2Int[] CardinalNeighbours =
        {
            new Vector2Int(0, 1), new Vector2Int(1, 0),
            new Vector2Int(0, -1), new Vector2Int(-1, 0)
        };

        [SerializeField] private float moveSpeed = 2.15f;
        [SerializeField] private bool autonomous = true;
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private bool tintBodyByState = true;

        private readonly List<Vector2Int> path = new List<Vector2Int>();
        private CharacterController controller;
        private MaterialPropertyBlock bodyProperties;
        [SerializeField] private FireField field;
        [SerializeField] private Transform exit;
        [SerializeField] private ObjectFire[] objectFires = Array.Empty<ObjectFire>();
        [SerializeField] private NpcItemRequest escapeRequirement;
        private float routeRefreshTimer;
        private float stuckTimer;
        private int pathIndex;
        private State state = State.WaitingForSafeRoute;

        public bool IsRescued => state == State.Rescued;
        public bool HasSafePath { get; private set; }
        public bool Autonomous
        {
            get => autonomous;
            set => autonomous = value;
        }

        public State CurrentState => state;

        public event Action<RescueNpc> Rescued;

        public void SetEscapeRequirement(NpcItemRequest requirement) => escapeRequirement = requirement;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            controller.minMoveDistance = 0;
            controller.radius = 0.28f;
            controller.height = 1.5f;
            controller.center = new Vector3(0f, 0.75f, 0f);

            if (bodyRenderer == null)
            {
                bodyRenderer = GetComponentInChildren<Renderer>();
            }

            bodyProperties = new MaterialPropertyBlock();
            RefreshVisual();
        }

        private void Update()
        {
            if (autonomous)
            {
                Tick(Time.deltaTime);
            }
        }

        /// <summary>Supplies scene-owned dependencies and resets only route state.</summary>
        public void Initialize(FireField fireField, Transform exitTransform)
        {
            field = fireField;
            exit = exitTransform;
            path.Clear();
            pathIndex = 0;
            HasSafePath = false;
            routeRefreshTimer = 0f;
            stuckTimer = 0f;

            if (!IsRescued)
            {
                SetState(State.WaitingForSafeRoute);
            }
        }

        public void Initialize(FireField fireField, Transform exitTransform, ObjectFire[] fires)
        {
            objectFires = fires ?? Array.Empty<ObjectFire>();
            Initialize(fireField, exitTransform);
        }

        /// <summary>
        /// Advances route discovery and movement.  It is public so a later network owner
        /// can drive the same deterministic state transitions instead of Update.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (deltaTime < 0f)
            {
                return;
            }

            if (IsRescued || field == null || exit == null)
            {
                return;
            }

            if (escapeRequirement && !escapeRequirement.CanLeave)
            {
                InvalidateRoute();
                return;
            }

            if (!field.TryGetCell(transform.position, out Vector2Int currentCell) || !IsPassable(currentCell))
            {
                HasSafePath = false;
                path.Clear();
                pathIndex = 0;
                SetState(State.WaitingForSafeRoute);
                return;
            }

            routeRefreshTimer -= Mathf.Max(0f, deltaTime);
            if (routeRefreshTimer <= 0f)
            {
                if (!HasSafePath || !RemainingRouteIsPassable())
                {
                    RebuildRoute();
                }

                routeRefreshTimer = RouteRefreshInterval;
            }

            if (!HasSafePath)
            {
                SetState(State.WaitingForSafeRoute);
                return;
            }

            if (state != State.Escaping)
            {
                SetState(State.Escaping);
            }

            MoveAlongRoute(deltaTime);
        }

        private void RebuildRoute()
        {
            path.Clear();
            pathIndex = 0;
            HasSafePath = false;

            if (!field.TryGetCell(transform.position, out Vector2Int start) ||
                !field.TryGetCell(exit.position, out Vector2Int goal) ||
                !IsPassable(start) ||
                !IsPassable(goal))
            {
                return;
            }

            int width = field.Width;
            int height = field.Height;
            int cellCount = width * height;
            int[] previous = new int[cellCount];
            bool[] visited = new bool[cellCount];
            for (int i = 0; i < previous.Length; i++)
            {
                previous[i] = -1;
            }

            Queue<Vector2Int> frontier = new Queue<Vector2Int>();
            frontier.Enqueue(start);
            visited[ToIndex(start, width)] = true;

            bool found = false;
            while (frontier.Count > 0)
            {
                Vector2Int current = frontier.Dequeue();
                if (current == goal)
                {
                    found = true;
                    break;
                }

                for (int i = 0; i < CardinalNeighbours.Length; i++)
                {
                    Vector2Int next = current + CardinalNeighbours[i];
                    if (!InBounds(next, width, height))
                    {
                        continue;
                    }

                    int nextIndex = ToIndex(next, width);
                    if (visited[nextIndex] || !IsPassable(next) || !IsTransitionPassable(current, next))
                    {
                        continue;
                    }

                    visited[nextIndex] = true;
                    previous[nextIndex] = ToIndex(current, width);
                    frontier.Enqueue(next);
                }
            }

            if (!found)
            {
                return;
            }

            int index = ToIndex(goal, width);
            int startIndex = ToIndex(start, width);
            while (index != startIndex)
            {
                path.Add(FromIndex(index, width));
                index = previous[index];
            }

            path.Reverse();
            // Going through the current cell centre before an adjacent centre ensures a
            // four-neighbour path cannot visually cut a wall corner from an off-centre start.
            path.Insert(0, start);
            HasSafePath = true;
        }

        private void MoveAlongRoute(float deltaTime)
        {
            if (pathIndex >= path.Count)
            {
                Rescue();
                return;
            }

            Vector2Int nextCell = path[pathIndex];
            // Re-check just ahead of movement so a fresh fire stops the NPC immediately,
            // rather than waiting for the next scheduled BFS refresh.
            if (!IsPassable(nextCell))
            {
                InvalidateRoute();
                return;
            }

            Vector3 destination = field.CellCenter(nextCell.x, nextCell.y);
            destination.y = transform.position.y;
            Vector3 toDestination = destination - transform.position;
            toDestination.y = 0f;
            float remaining = toDestination.magnitude;
            float step = moveSpeed * Mathf.Max(0f, deltaTime);

            if (remaining <= Mathf.Max(0.04f, step))
            {
                if (!IsMoveClear(transform.position, destination))
                {
                    InvalidateRoute();
                    return;
                }

                Vector3 before = transform.position;
                controller.Move(toDestination);
                Vector3 afterMove = destination - transform.position;
                afterMove.y = 0f;
                if (afterMove.sqrMagnitude <= 0.08f * 0.08f)
                {
                    pathIndex++;
                    stuckTimer = 0f;
                }
                else
                {
                    TrackMovement(before, toDestination.magnitude, deltaTime);
                }

                return;
            }

            Vector3 direction = toDestination / remaining;
            transform.forward = Vector3.Slerp(transform.forward, direction, Mathf.Clamp01(deltaTime * 12f));
            Vector3 target = transform.position + direction * step;
            if (!IsMoveClear(transform.position, target))
            {
                InvalidateRoute();
                return;
            }

            Vector3 beforeMove = transform.position;
            controller.Move(direction * step);
            TrackMovement(beforeMove, step, deltaTime);
        }

        private bool IsPassable(Vector2Int cell)
        {
            if (!field.IsSafe(cell))
            {
                return false;
            }

            Vector3 center = field.CellCenter(cell.x, cell.y);
            if (objectFires != null)
            {
                foreach (ObjectFire fire in objectFires)
                    if (fire && fire.BlocksEscapeAt(center, ClearanceRadius)) return false;
            }

            GetCapsule(center, out Vector3 bottom, out Vector3 top, out float radius);
            return !Physics.CheckCapsule(bottom, top, radius, FixedObstacleMask, QueryTriggerInteraction.Ignore);
        }

        private bool IsTransitionPassable(Vector2Int from, Vector2Int to)
        {
            Vector3 fromCenter = field.CellCenter(from.x, from.y);
            Vector3 toCenter = field.CellCenter(to.x, to.y);
            return IsMoveClear(fromCenter, toCenter);
        }

        /// <summary>
        /// Tests the swept controller shape, rather than only the next cell centre.  This
        /// prevents the graph from accepting a diagonal-looking corner squeeze that the
        /// actual controller will collide with while walking between cardinal cells.
        /// </summary>
        private bool IsMoveClear(Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            delta.y = 0f;
            float distance = delta.magnitude;
            if (distance <= 0.0001f)
            {
                return true;
            }

            GetCapsule(from, out Vector3 bottom, out Vector3 top, out float radius);
            return !Physics.CapsuleCast(bottom, top, radius, delta / distance, distance,
                FixedObstacleMask, QueryTriggerInteraction.Ignore);
        }

        private float ClearanceRadius => controller != null ? controller.radius + ClearancePadding : .315f;

        private void GetCapsule(Vector3 feet, out Vector3 bottom, out Vector3 top, out float radius)
        {
            radius = ClearanceRadius;
            float height = controller != null ? Mathf.Max(controller.height, radius * 2f) : 1.5f;
            float vertical = Mathf.Max(0f, height * .5f - radius);
            Vector3 center = feet + Vector3.up * (controller != null ? controller.center.y : height * .5f);
            bottom = center - Vector3.up * vertical;
            top = center + Vector3.up * vertical;
            // The authored front aperture has a 2.7mm sill. The controller can step
            // over it, but a floor-grazing inflated query rejected the entire doorway.
            // Trim only the lowest 25mm, not walls/door width or the actual controller.
            bottom += Vector3.up * (controller ? Mathf.Min(.025f,controller.stepOffset) : .025f);
        }

        private void TrackMovement(Vector3 before, float intendedDistance, float deltaTime)
        {
            Vector3 travelled = transform.position - before;
            travelled.y = 0f;
            if (travelled.magnitude >= Mathf.Min(.015f, intendedDistance * .35f))
            {
                stuckTimer = 0f;
                return;
            }

            stuckTimer += Mathf.Max(0f, deltaTime);
            if (stuckTimer >= StuckRepathDelay)
            {
                InvalidateRoute();
            }
        }

        private void InvalidateRoute()
        {
            HasSafePath = false;
            path.Clear();
            pathIndex = 0;
            stuckTimer = 0f;
            routeRefreshTimer = 0f;
            SetState(State.WaitingForSafeRoute);
        }

        private bool RemainingRouteIsPassable()
        {
            for (int i = pathIndex; i < path.Count; i++)
            {
                if (!IsPassable(path[i]))
                {
                    return false;
                }

                if (i > pathIndex && !IsTransitionPassable(path[i - 1], path[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private void Rescue()
        {
            if (IsRescued)
            {
                return;
            }

            HasSafePath = false;
            path.Clear();
            SetState(State.Rescued);
            Rescued?.Invoke(this);
            gameObject.SetActive(false);
        }

        private void SetState(State next)
        {
            if (state == next)
            {
                return;
            }

            state = next;
            RefreshVisual();
        }

        private void RefreshVisual()
        {
            if (bodyRenderer == null || !tintBodyByState)
            {
                return;
            }

            Color color = state switch
            {
                State.WaitingForSafeRoute => new Color(1f, 0.42f, 0.12f),
                State.Escaping => new Color(0.22f, 0.85f, 0.38f),
                _ => new Color(0.45f, 0.8f, 1f)
            };

            bodyRenderer.GetPropertyBlock(bodyProperties);
            bodyProperties.SetColor("_Color", color);
            bodyProperties.SetColor("_BaseColor", color);
            bodyRenderer.SetPropertyBlock(bodyProperties);
        }

        private static bool InBounds(Vector2Int cell, int width, int height)
        {
            return cell.x >= 0 && cell.x < width && cell.y >= 0 && cell.y < height;
        }

        private static int ToIndex(Vector2Int cell, int width)
        {
            return cell.y * width + cell.x;
        }

        private static Vector2Int FromIndex(int index, int width)
        {
            return new Vector2Int(index % width, index / width);
        }
    }
}
