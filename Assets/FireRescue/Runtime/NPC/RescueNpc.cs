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
        private float routeRefreshTimer;
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
                    if (visited[nextIndex] || !IsPassable(next))
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
                HasSafePath = false;
                path.Clear();
                pathIndex = 0;
                SetState(State.WaitingForSafeRoute);
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
                controller.Move(toDestination);
                Vector3 afterMove = destination - transform.position;
                afterMove.y = 0f;
                if (afterMove.sqrMagnitude <= 0.08f * 0.08f)
                {
                    pathIndex++;
                }

                return;
            }

            Vector3 direction = toDestination / remaining;
            transform.forward = Vector3.Slerp(transform.forward, direction, Mathf.Clamp01(deltaTime * 12f));
            controller.Move(direction * step);
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
                    if (fire && fire.BlocksEscapeAt(center, .28f)) return false;
            }
            Vector3 halfExtents = new Vector3(field.CellSize * 0.38f, 0.7f, field.CellSize * 0.38f);
            center.y += halfExtents.y;
            return !Physics.CheckBox(center, halfExtents, Quaternion.identity, FixedObstacleMask, QueryTriggerInteraction.Ignore);
        }

        private bool RemainingRouteIsPassable()
        {
            for (int i = pathIndex; i < path.Count; i++)
            {
                if (!IsPassable(path[i]))
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
