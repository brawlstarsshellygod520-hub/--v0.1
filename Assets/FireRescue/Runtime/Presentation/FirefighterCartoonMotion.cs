using System;
using UnityEngine;

namespace FireRescue
{
    /// <summary>Distance-driven toy gait and two-bone grip posing. Never moves the actor/colliders/nozzle gameplay anchors.</summary>
    [DefaultExecutionOrder(50)]
    public sealed class FirefighterCartoonMotion : MonoBehaviour
    {
        [Serializable] public sealed class RestBone
        {public Transform bone;public Vector3 position;public Quaternion rotation;}
        [SerializeField] Transform actor, hips, chest, head;
        Transform spine;
        Vector3 spineRest;
        [SerializeField] Transform leftThigh,leftShin,leftFoot,rightThigh,rightShin,rightFoot;
        [SerializeField] Transform leftUpper,leftLower,leftHand,rightUpper,rightLower,rightHand;
        [SerializeField] Transform nozzleMount,axePivot;
        [SerializeField] FirefighterTools tools;
        [SerializeField] PlayerCarryInteraction carry;
        [SerializeField] RestBone[] rest;
        [SerializeField] Vector3 leftFootRest,rightFootRest;
        [SerializeField] Quaternion leftFootRotation,rightFootRotation;
        [SerializeField] float stride=.92f;
        Vector3 lastPosition;
        Vector3 gaitDirection=Vector3.forward;
        float phase,amount,turnLean,lastYaw;
        public float TravelledDistance {get;private set;}
        public float GaitPhase => phase;
        public float MovementBlend => amount;
        public float LeftGripError {get;private set;}
        public float RightGripError {get;private set;}
        public void Initialize(Transform root,Transform rig,FirefighterTools toolOwner,PlayerCarryInteraction itemOwner,Transform nozzle,Transform axe)
        {
            actor=root;tools=toolOwner;carry=itemOwner;nozzleMount=nozzle;axePivot=axe;
            var all=rig.GetComponentsInChildren<Transform>(true);
            Transform Find(string n)=>Array.Find(all,t=>t.name==n);
            hips=Find("Hips");chest=Find("Chest");head=Find("Head");
            leftThigh=Find("L_Thigh");leftShin=Find("L_Shin");leftFoot=Find("L_Foot");
            rightThigh=Find("R_Thigh");rightShin=Find("R_Shin");rightFoot=Find("R_Foot");
            leftUpper=Find("L_UpperArm");leftLower=Find("L_LowerArm");leftHand=Find("L_Hand");
            rightUpper=Find("R_UpperArm");rightLower=Find("R_LowerArm");rightHand=Find("R_Hand");
            NormalizeArmSides();
            var bones=new[]{hips,chest,head,leftThigh,leftShin,leftFoot,rightThigh,rightShin,rightFoot,leftUpper,leftLower,leftHand,rightUpper,rightLower,rightHand};
            rest=Array.ConvertAll(bones,b=>new RestBone{bone=b,position=b.localPosition,rotation=b.localRotation});
            leftFootRest=actor.InverseTransformPoint(leftFoot.position);rightFootRest=actor.InverseTransformPoint(rightFoot.position);
            leftFootRotation=Quaternion.Inverse(actor.rotation)*leftFoot.rotation;rightFootRotation=Quaternion.Inverse(actor.rotation)*rightFoot.rotation;
        }
        void Awake(){NormalizeArmSides();spine=chest?chest.parent:null;if(spine)spineRest=spine.localPosition;}
        void NormalizeArmSides()
        {
            // FBX face correction rotates this user's rig 180 degrees; L_/R_ names are
            // opposite to actor-space X. Use actual shoulder positions, never cross arms.
            if(!actor||!leftUpper||actor.InverseTransformPoint(leftUpper.position).x<0)return;
            (leftUpper,rightUpper)=(rightUpper,leftUpper);(leftLower,rightLower)=(rightLower,leftLower);(leftHand,rightHand)=(rightHand,leftHand);
        }
        void OnEnable(){lastPosition=actor?actor.position:transform.position;lastYaw=actor?actor.eulerAngles.y:0;phase=amount=turnLean=0;}
        void LateUpdate()=>TickPresentation(Time.deltaTime);
        public void TickPresentation(float dt)
        {
            if(!actor||rest==null||dt<=0)return;
            Vector3 delta=actor.position-lastPosition;delta.y=0;lastPosition=actor.position;
            float distance=delta.magnitude;if(distance/dt>16)distance=0;
            if(distance>.0001f)gaitDirection=actor.InverseTransformDirection(delta.normalized);
            float speed=distance/dt;float blend=1-Mathf.Exp(-18*dt);
            amount=Mathf.Lerp(amount,Mathf.Clamp01(speed/1.2f),blend);
            phase=Mathf.Repeat(phase+distance/stride,1);TravelledDistance+=distance;
            float yaw=actor.eulerAngles.y;float rate=Mathf.Clamp(Mathf.DeltaAngle(lastYaw,yaw)/Mathf.Max(dt,.001f)/600,-1,1);lastYaw=yaw;
            turnLean=Mathf.Lerp(turnLean,rate,1-Mathf.Exp(-12*dt));
            foreach(var b in rest){b.bone.localPosition=b.position;b.bone.localRotation=b.rotation;}
            if(spine)spine.localPosition=spineRest;
            float wave=Mathf.Sin(phase*Mathf.PI*2),bounce=(1-Mathf.Cos(phase*Mathf.PI*4))*.018f*amount;
            hips.position+=Vector3.up*(-.09f*amount+bounce+Mathf.Sin(Time.time*3)*.003f*(1-amount));
            // Flex the short legs without bringing the oversized head down onto the nozzle.
            if(spine)spine.position+=Vector3.up*(.09f*amount);
            hips.rotation=Quaternion.AngleAxis(wave*3*amount-turnLean*5,actor.forward)*hips.rotation;
            Vector3 localMove=actor.InverseTransformDirection(delta/Mathf.Max(dt,.001f));
            chest.rotation=Quaternion.AngleAxis(Mathf.Clamp(localMove.z*1.4f,-6,6),actor.right)*chest.rotation;
            head.rotation=Quaternion.AngleAxis(-wave*1.8f*amount,actor.forward)*head.rotation;
            PoseLeg(leftThigh,leftShin,leftFoot,leftFootRest,leftFootRotation,phase);
            PoseLeg(rightThigh,rightShin,rightFoot,rightFootRest,rightFootRotation,Mathf.Repeat(phase+.5f,1));
            // Subtle tool sway stays below 2cm; nozzle and visible jet origin remain aligned.
            bool hose=tools&&tools.SelectedTool==FirefighterTools.Tool.Hose;
            bool axe=tools&&tools.SelectedTool==FirefighterTools.Tool.Axe;
            bool carrying=carry&&carry.IsCarrying;
            Vector3 leftTarget,rightTarget;
            if(hose)
            {
                nozzleMount.localPosition=new Vector3(.02f,.76f+bounce*.25f,.23f);
                nozzleMount.localRotation=Quaternion.Euler(0,0,-turnLean*1.5f);
                leftTarget=actor.TransformPoint(new Vector3(-.10f,.75f+bounce*.25f,.20f));
                rightTarget=actor.TransformPoint(new Vector3(.10f,.74f+bounce*.25f,.18f));
            }
            else if(axe)
            {
                float swing=tools.IsSwinging?Mathf.Sin(tools.SwingProgress*Mathf.PI):0;
                axePivot.localPosition=new Vector3(.33f,.70f+bounce*.4f,.22f);
                // Carry the blade outward and forward of the oversized toy head.
                axePivot.localRotation=Quaternion.Euler(18+100*swing,0,-35+wave*3*amount);
                rightTarget=axePivot.position;
                leftTarget=tools.IsSwinging?axePivot.TransformPoint(new Vector3(0,.09f,0)):
                    actor.TransformPoint(new Vector3(-.32f,.48f,.05f+wave*.09f*amount));
            }
            else if(carrying)
            {leftTarget=actor.TransformPoint(new Vector3(-.10f,.76f,.23f));rightTarget=actor.TransformPoint(new Vector3(.10f,.76f,.23f));}
            else
            {
                leftTarget=actor.TransformPoint(new Vector3(-.35f,.48f,wave*.13f*amount));
                rightTarget=actor.TransformPoint(new Vector3(.35f,.48f,-wave*.13f*amount));
            }
            Solve(leftUpper,leftLower,leftHand,leftTarget,-actor.right+actor.forward*.2f);
            Solve(rightUpper,rightLower,rightHand,rightTarget,actor.right+actor.forward*.2f);
            LeftGripError=Vector3.Distance(leftHand.position,leftTarget);RightGripError=Vector3.Distance(rightHand.position,rightTarget);
        }
        void PoseLeg(Transform a,Transform b,Transform foot,Vector3 restFoot,Quaternion rotation,float cycle)
        {
            // The stance half travels backwards at exactly the actor's distance rate.
            float z=cycle<.5f?stride*(.25f-cycle):Mathf.Lerp(-stride*.25f,stride*.25f,Mathf.SmoothStep(0,1,(cycle-.5f)*2));
            float lift=cycle<.5f?0:Mathf.Sin((cycle-.5f)*Mathf.PI*2)*.055f;
            Vector3 target=actor.TransformPoint(restFoot+(gaitDirection*z+Vector3.up*lift)*amount);
            Solve(a,b,foot,target,actor.forward);
            foot.rotation=actor.rotation*rotation;
        }
        // Exact limb lengths, with a stable local pole. No transform scale/mesh stretching.
        static void Solve(Transform a,Transform b,Transform c,Vector3 target,Vector3 pole)
        {
            float l1=Vector3.Distance(a.position,b.position),l2=Vector3.Distance(b.position,c.position);
            Vector3 d=target-a.position;float distance=Mathf.Clamp(d.magnitude,.005f,(l1+l2)*.998f);
            Vector3 axis=d.normalized;
            Vector3 bend=Vector3.ProjectOnPlane(pole,axis).normalized;
            if(bend.sqrMagnitude<.001f)bend=Vector3.ProjectOnPlane(Vector3.forward,axis).normalized;
            float along=(distance*distance+l1*l1-l2*l2)/(2*distance);
            Vector3 elbow=a.position+axis*along+bend*Mathf.Sqrt(Mathf.Max(0,l1*l1-along*along));
            a.rotation=Quaternion.FromToRotation(b.position-a.position,elbow-a.position)*a.rotation;
            Vector3 reachable=a.position+axis*distance;
            b.rotation=Quaternion.FromToRotation(c.position-b.position,reachable-b.position)*b.rotation;
        }
    }
}
