// Copyright (c) BizSim Game Studios. All rights reserved.

using System.Runtime.CompilerServices;

// Allow the optional InputSystem support assembly to access internal members
// (e.g., InstallReferrerDebugMenu.KeyToggleCheck / TouchBeganCheck callbacks).
[assembly: InternalsVisibleTo("BizSim.Google.Play.InstallReferrer.InputSystem")]
[assembly: InternalsVisibleTo("BizSim.Google.Play.InstallReferrer.Editor")]
[assembly: InternalsVisibleTo("BizSim.Google.Play.InstallReferrer.EditorTests")]
[assembly: InternalsVisibleTo("BizSim.Google.Play.InstallReferrer.Tests")]
