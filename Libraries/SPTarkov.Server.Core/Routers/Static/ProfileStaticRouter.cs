using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Callbacks;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Launcher;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Utils;

namespace SPTarkov.Server.Core.Routers.Static;

[Injectable]
public class ProfileStaticRouter : StaticRouter
{
    public ProfileStaticRouter(ProfileCallbacks profileCallbacks, JsonUtil jsonUtil) : base(
        jsonUtil,
        [
            new RouteAction(
                "/client/game/profile/create",
                (
                    url,
                    info,
                    sessionID,
                    output
                ) => profileCallbacks.CreateProfile(url, info as ProfileCreateRequestData, sessionID),
                typeof(ProfileCreateRequestData)
            ),
            new RouteAction(
                "/client/game/profile/list",
                (
                    url,
                    info,
                    sessionID,
                    output
                ) => profileCallbacks.GetProfileData(url, info as EmptyRequestData, sessionID)
            ),
            new RouteAction(
                "/client/game/profile/savage/regenerate",
                (
                    url,
                    info,
                    sessionID,
                    output
                ) => profileCallbacks.RegenerateScav(url, info as EmptyRequestData, sessionID)
            ),
            new RouteAction(
                "/client/game/profile/voice/change",
                (url, info, sessionID, output) =>
                    profileCallbacks.ChangeVoice(url, info as ProfileChangeVoiceRequestData, sessionID),
                typeof(ProfileChangeVoiceRequestData)
            ),
            new RouteAction(
                "/client/game/profile/nickname/change",
                (
                    url,
                    info,
                    sessionID,
                    output
                ) => profileCallbacks.ChangeNickname(url, info as ProfileChangeNicknameRequestData, sessionID),
                typeof(ProfileChangeNicknameRequestData)
            ),
            new RouteAction(
                "/client/game/profile/nickname/validate",
                (
                    url,
                    info,
                    sessionID,
                    output
                ) => profileCallbacks.ValidateNickname(url, info as ValidateNicknameRequestData, sessionID),
                typeof(ValidateNicknameRequestData)
            ),
            new RouteAction(
                "/client/game/profile/nickname/reserved",
                (
                    url,
                    info,
                    sessionID,
                    output
                ) => profileCallbacks.GetReservedNickname(url, info as EmptyRequestData, sessionID)
            ),
            new RouteAction(
                "/client/profile/status",
                (
                    url,
                    info,
                    sessionID,
                    output
                ) => profileCallbacks.GetProfileStatus(url, info as EmptyRequestData, sessionID)
            ),
            new RouteAction(
                "/client/profile/view",
                (
                    url,
                    info,
                    sessionID,
                    output
                ) => profileCallbacks.GetOtherProfile(url, info as GetOtherProfileRequest, sessionID),
                typeof(GetOtherProfileRequest)
            ),
            new RouteAction(
                "/client/profile/settings",
                (
                    url,
                    info,
                    sessionID,
                    output
                ) => profileCallbacks.GetProfileSettings(url, info as GetProfileSettingsRequest, sessionID),
                typeof(GetProfileSettingsRequest)
            ),
            new RouteAction(
                "/client/game/profile/search",
                (
                    url,
                    info,
                    sessionID,
                    output
                ) => profileCallbacks.SearchProfiles(url, info as SearchProfilesRequestData, sessionID),
                typeof(SearchProfilesRequestData)
            ),
            new RouteAction(
                "/launcher/profile/info",
                (url, info, sessionID, output) =>
                    profileCallbacks.GetMiniProfile(url, info as GetMiniProfileRequestData, sessionID),
                typeof(GetMiniProfileRequestData)
            ),
            new RouteAction(
                "/launcher/profiles",
                (url, info, sessionID, output) =>
                    profileCallbacks.GetAllMiniProfiles(url, info as EmptyRequestData, sessionID)
            )
        ]
    )
    {
    }
}
