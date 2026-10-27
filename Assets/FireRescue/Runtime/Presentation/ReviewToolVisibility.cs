using UnityEngine;
namespace FireRescue
{
    // Only the new rig presentation owns poses; legacy tools view is preserved for old scenes.
    public sealed class ReviewToolVisibility:MonoBehaviour
    {
        [SerializeField] FirefighterTools tools;
        [SerializeField] GameObject axe,nozzle;
        public void Initialize(FirefighterTools value,GameObject axeModel,GameObject nozzleModel){tools=value;axe=axeModel;nozzle=nozzleModel;}
        void LateUpdate(){if(axe)axe.SetActive(tools&&tools.SelectedTool==FirefighterTools.Tool.Axe);if(nozzle)nozzle.SetActive(tools&&tools.SelectedTool==FirefighterTools.Tool.Hose);}
        void OnDisable(){if(axe)axe.SetActive(false);if(nozzle)nozzle.SetActive(false);}
    }
}
