using UnityEditor;

namespace BizSim.Google.Play.InstallReferrer.Editor
{
    [InitializeOnLoad]
    static class InstallReferrerEditorInit
    {
        static InstallReferrerEditorInit()
        {
            BizSim.Google.Play.Editor.Core.BizSimDefineManager.AddDefine(
                "BIZSIM_INSTALLREFERRER_INSTALLED",
                BizSim.Google.Play.Editor.Core.BizSimDefineManager.GetRelevantPlatforms());
        }
    }
}
