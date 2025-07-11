using System.Collections.Frozen;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Cloners;
using LogLevel = SPTarkov.Server.Core.Models.Spt.Logging.LogLevel;

namespace SPTarkov.Server.Core.Helpers;

[Injectable]
public class ProfileHelper(
    ISptLogger<ProfileHelper> _logger,
    ICloner _cloner,
    SaveServer _saveServer,
    DatabaseService _databaseService,
    Watermark _watermark,
    TimeUtil _timeUtil,
    ServerLocalisationService _serverLocalisationService,
    ConfigServer _configServer
)
{
    protected static readonly FrozenSet<string> _gameEditionsWithFreeRefresh =
    [
        "edge_of_darkness",
        "unheard_edition",
    ];
    protected readonly InventoryConfig _inventoryConfig =
        _configServer.GetConfig<InventoryConfig>();

    /// <summary>
    ///     Remove/reset a completed quest condition from players profile quest data
    /// </summary>
    /// <param name="sessionID">Session id</param>
    /// <param name="questConditionId">Quest with condition to remove</param>
    public void RemoveQuestConditionFromProfile(
        PmcData pmcData,
        Dictionary<string, string> questConditionId
    )
    {
        foreach (var questId in questConditionId)
        {
            var conditionId = questId.Value;
            var profileQuest = pmcData.Quests.FirstOrDefault(q => q.QId == conditionId);

            if (profileQuest != null) // Remove condition
            {
                profileQuest.CompletedConditions.Remove(conditionId);
            }
        }
    }

    /// <summary>
    ///     Get all profiles from server
    /// </summary>
    /// <returns>Dictionary of profiles</returns>
    public Dictionary<string, SptProfile> GetProfiles()
    {
        return _saveServer.GetProfiles();
    }

    /// <summary>
    ///     Get the pmc and scav profiles as an array by profile id
    /// </summary>
    /// <param name="sessionId">Session/Player id</param>
    /// <returns>Array of PmcData objects</returns>
    public List<PmcData> GetCompleteProfile(string sessionId)
    {
        var output = new List<PmcData>();

        if (IsWiped(sessionId))
        {
            return output;
        }

        var FullProfileClone = _cloner.Clone(GetFullProfile(sessionId));

        // Sanitize any data the client can not receive
        SanitizeProfileForClient(FullProfileClone);

        // PMC must be at array index 0, scav at 1
        output.Add(FullProfileClone.CharacterData.PmcData);
        output.Add(FullProfileClone.CharacterData.ScavData);

        return output;
    }

    /// <summary>
    ///     Sanitize any information from the profile that the client does not expect to receive
    /// </summary>
    /// <param name="clonedProfile">A clone of the full player profile</param>
    protected void SanitizeProfileForClient(SptProfile clonedProfile)
    {
        // Remove `loyaltyLevel` from `TradersInfo`, as otherwise it causes the client to not
        // properly calculate the player's `loyaltyLevel`
        foreach (var trader in clonedProfile.CharacterData.PmcData.TradersInfo.Values)
        {
            trader.LoyaltyLevel = null;
        }
    }

    /// <summary>
    ///     Check if a nickname is used by another profile loaded by the server
    /// </summary>
    /// <param name="nicknameRequest">nickname request object</param>
    /// <param name="sessionID">Session id</param>
    /// <returns>True if already in use</returns>
    public bool IsNicknameTaken(ValidateNicknameRequestData nicknameRequest, string sessionID)
    {
        var allProfiles = _saveServer.GetProfiles().Values;

        // Find a profile that doesn't have same session id but has same name
        return allProfiles.Any(p =>
            ProfileHasInfoProperty(p)
            && !StringsMatch(p.ProfileInfo.ProfileId, sessionID)
            && // SessionIds dont match
            StringsMatch(
                p.CharacterData.PmcData.Info.LowerNickname.ToLowerInvariant(),
                nicknameRequest.Nickname.ToLowerInvariant()
            )
        ); // Nicknames do
    }

    protected bool ProfileHasInfoProperty(SptProfile profile)
    {
        return profile?.CharacterData?.PmcData?.Info != null;
    }

    protected bool StringsMatch(string stringA, string stringB)
    {
        return stringA == stringB;
    }

    /// <summary>
    ///     Add experience to a PMC inside the players profile
    /// </summary>
    /// <param name="sessionID">Session id</param>
    /// <param name="experienceToAdd">Experience to add to PMC character</param>
    public void AddExperienceToPmc(string sessionID, int experienceToAdd)
    {
        var pmcData = GetPmcProfile(sessionID);
        if (pmcData != null)
        {
            pmcData.Info.Experience += experienceToAdd;
        }
        else
        {
            _logger.Error($"Profile {sessionID} does not exist");
        }
    }

    /// <summary>
    ///     Iterate all profiles and find matching pmc profile by provided id
    /// </summary>
    /// <param name="pmcId">Profile id to find</param>
    /// <returns>PmcData</returns>
    public PmcData? GetProfileByPmcId(string pmcId)
    {
        return _saveServer
            .GetProfiles()
            .Values.First(p => p.CharacterData?.PmcData?.Id == pmcId)
            .CharacterData.PmcData;
    }

    /// <summary>
    ///     Get experience value for given level
    /// </summary>
    /// <param name="level">Level to get xp for</param>
    /// <returns>Number of xp points for level</returns>
    public int? GetExperience(int level)
    {
        var playerLevel = level;
        var expTable = _databaseService.GetGlobals().Configuration.Exp.Level.ExperienceTable;
        int? exp = 0;

        if (playerLevel >= expTable.Length) // make sure to not go out of bounds
        {
            playerLevel = expTable.Length - 1;
        }

        for (var i = 0; i < playerLevel; i++)
        {
            exp += expTable[i].Experience;
        }

        return exp;
    }

    /// <summary>
    ///     Get the max level a player can be
    /// </summary>
    /// <returns>Max level</returns>
    public int GetMaxLevel()
    {
        return _databaseService.GetGlobals().Configuration.Exp.Level.ExperienceTable.Length - 1;
    }

    /// <summary>
    ///     Get default Spt data object
    /// </summary>
    /// <returns>Spt</returns>
    public Spt GetDefaultSptDataObject()
    {
        return new Spt
        {
            Version = _watermark.GetVersionTag(true),
            Mods = [],
            ReceivedGifts = [],
            BlacklistedItemTemplates = [],
            FreeRepeatableRefreshUsedCount = new Dictionary<string, int>(),
            Migrations = new Dictionary<string, long>(),
            CultistRewards = new Dictionary<string, AcceptedCultistReward>(),
            PendingPrestige = null,
            ExtraRepeatableQuests = new Dictionary<string, double>(),
        };
    }

    /// <summary>
    ///     Get full representation of a players profile json
    /// </summary>
    /// <param name="sessionID">Profile id to get</param>
    /// <returns>SptProfile object</returns>
    public SptProfile? GetFullProfile(string sessionID)
    {
        return _saveServer.ProfileExists(sessionID) ? _saveServer.GetProfile(sessionID) : null;
    }

    /// <summary>
    ///     Get full representation of a players profile JSON by the account ID, or undefined if not found
    /// </summary>
    /// <param name="accountId">Account ID to find</param>
    /// <returns></returns>
    public SptProfile? GetFullProfileByAccountId(string accountId)
    {
        var check = int.TryParse(accountId, out var aid);
        if (!check)
        {
            _logger.Error($"Account {accountId} does not exist");
        }

        return _saveServer
            .GetProfiles()
            .FirstOrDefault(p => p.Value?.ProfileInfo?.Aid == aid)
            .Value;
    }

    /// <summary>
    ///     Retrieve a ChatRoomMember formatted profile for the given session ID
    /// </summary>
    /// <param name="sessionID">The session ID to return the profile for</param>
    /// <returns></returns>
    public SearchFriendResponse? GetChatRoomMemberFromSessionId(string sessionID)
    {
        var pmcProfile = GetFullProfile(sessionID)?.CharacterData?.PmcData;
        if (pmcProfile == null)
        {
            return null;
        }

        return GetChatRoomMemberFromPmcProfile(pmcProfile);
    }

    /// <summary>
    ///     Retrieve a ChatRoomMember formatted profile for the given PMC profile data
    /// </summary>
    /// <param name="pmcProfile">The PMC profile data to format into a ChatRoomMember structure</param>
    /// <returns></returns>
    public SearchFriendResponse? GetChatRoomMemberFromPmcProfile(PmcData pmcProfile)
    {
        return new SearchFriendResponse
        {
            Id = pmcProfile.Id,
            Aid = pmcProfile.Aid,
            Info = new UserDialogDetails
            {
                Nickname = pmcProfile.Info.Nickname,
                Side = pmcProfile.Info.Side,
                Level = pmcProfile.Info.Level,
                MemberCategory = pmcProfile.Info.MemberCategory,
                SelectedMemberCategory = pmcProfile.Info.SelectedMemberCategory,
            },
        };
    }

    /// <summary>
    ///     Get a PMC profile by its session id
    /// </summary>
    /// <param name="sessionID">Profile id to return</param>
    /// <returns>PmcData object</returns>
    public PmcData? GetPmcProfile(string sessionID)
    {
        return GetFullProfile(sessionID)?.CharacterData?.PmcData;
    }

    /// <summary>
    ///     Is given user id a player
    /// </summary>
    /// <param name="userId">Id to validate</param>
    /// <returns>True is a player</returns>
    /// UNUSED?
    public bool IsPlayer(string userId)
    {
        return _saveServer.ProfileExists(userId);
    }

    /// <summary>
    ///     Get a full profiles scav-specific sub-profile
    /// </summary>
    /// <param name="sessionID">Profiles id</param>
    /// <returns>IPmcData object</returns>
    public PmcData? GetScavProfile(string sessionID)
    {
        return _saveServer.GetProfile(sessionID)?.CharacterData?.ScavData;
    }

    /// <summary>
    ///     Get baseline counter values for a fresh profile
    /// </summary>
    /// <returns>Default profile Stats object</returns>
    public Stats GetDefaultCounters()
    {
        return new Stats
        {
            Eft = new EftStats
            {
                CarriedQuestItems = [],
                DamageHistory = new DamageHistory
                {
                    LethalDamagePart = "Head",
                    LethalDamage = null,
                    BodyParts = new BodyPartsDamageHistory(),
                },
                DroppedItems = [],
                ExperienceBonusMult = 0,
                FoundInRaidItems = [],
                LastPlayerState = null,
                LastSessionDate = 0,
                OverallCounters = new OverallCounters { Items = [] },
                SessionCounters = new SessionCounters { Items = [] },
                SessionExperienceMult = 0,
                SurvivorClass = "Unknown",
                TotalInGameTime = 0,
                TotalSessionExperience = 0,
                Victims = [],
            },
        };
    }

    /// <summary>
    ///     is this profile flagged for data removal
    /// </summary>
    /// <param name="sessionID">Profile id</param>
    /// <returns>True if profile is to be wiped of data/progress</returns>
    /// TODO: logic doesn't feel right to have IsWiped being nullable
    protected bool IsWiped(string sessionID)
    {
        return _saveServer.GetProfile(sessionID)?.ProfileInfo?.IsWiped ?? false;
    }

    /// <summary>
    ///     Iterate over player profile inventory items and find the secure container and remove it
    /// </summary>
    /// <param name="profile">Profile to remove secure container from</param>
    /// <returns>profile without secure container</returns>
    public PmcData RemoveSecureContainer(PmcData profile)
    {
        var items = profile.Inventory.Items;
        var secureContainer = items.FirstOrDefault(i => i.SlotId == "SecuredContainer");
        if (secureContainer is not null)
        {
            // Find and remove container + children
            var childItemsInSecureContainer = items.FindAndReturnChildrenByItems(
                secureContainer.Id
            );

            // Remove child items + secure container
            profile.Inventory.Items = items
                .Where(i => !childItemsInSecureContainer.Contains(i.Id))
                .ToList();
        }

        return profile;
    }

    /// <summary>
    ///     Flag a profile as having received a gift
    ///     Store giftId in profile spt object
    /// </summary>
    /// <param name="playerId">Player to add gift flag to</param>
    /// <param name="giftId">Gift player received</param>
    /// <param name="maxCount">Limit of how many of this gift a player can have</param>
    public void FlagGiftReceivedInProfile(string playerId, string giftId, int maxCount)
    {
        var profileToUpdate = GetFullProfile(playerId);
        profileToUpdate.SptData.ReceivedGifts ??= [];

        var giftData = profileToUpdate.SptData.ReceivedGifts.FirstOrDefault(g =>
            g.GiftId == giftId
        );
        if (giftData != null)
        {
            // Increment counter
            giftData.Current++;
            return;
        }

        // Player has never received gift, make a new object
        profileToUpdate.SptData.ReceivedGifts.Add(
            new ReceivedGift
            {
                GiftId = giftId,
                TimestampLastAccepted = _timeUtil.GetTimeStamp(),
                Current = 1,
            }
        );
    }

    /// <summary>
    ///     Check if profile has received a gift by id
    /// </summary>
    /// <param name="playerId">Player profile to check for gift</param>
    /// <param name="giftId">Gift to check for</param>
    /// <param name="maxGiftCount">Max times gift can be given to player</param>
    /// <returns>True if player has received gift previously</returns>
    public bool PlayerHasReceivedMaxNumberOfGift(string playerId, string giftId, int maxGiftCount)
    {
        var profile = GetFullProfile(playerId);
        if (profile == null)
        {
            if (_logger.IsLogEnabled(LogLevel.Debug))
            {
                _logger.Debug($"Unable to gift {giftId}, Profile: {playerId} does not exist");
            }

            return false;
        }

        var giftDataFromProfile = profile.SptData.ReceivedGifts?.FirstOrDefault(g =>
            g.GiftId == giftId
        );
        if (giftDataFromProfile == null)
        {
            return false;
        }

        return giftDataFromProfile.Current >= maxGiftCount;
    }

    /// <summary>
    ///     Find Stat in profile counters and increment by one.
    /// </summary>
    /// <param name="counters">Counters to search for key</param>
    /// <param name="keyToIncrement">Key</param>
    /// Was Includes in Node so might not be exact?
    public void IncrementStatCounter(CounterKeyValue[] counters, string keyToIncrement)
    {
        var stat = counters.FirstOrDefault(c => c.Key.Contains(keyToIncrement));
        if (stat != null)
        {
            stat.Value++;
        }
    }

    /// <summary>
    ///     Check if player has a skill at elite level
    /// </summary>
    /// <param name="skill">Skill to check</param>
    /// <param name="pmcProfile">Profile to find skill in</param>
    /// <returns>True if player has skill at elite level</returns>
    public bool HasEliteSkillLevel(SkillTypes skill, PmcData pmcProfile)
    {
        var profileSkills = pmcProfile.Skills.Common;
        if (profileSkills == null)
        {
            return false;
        }

        var profileSkill = profileSkills.FirstOrDefault(s => s.Id == skill);
        if (profileSkill == null)
        {
            _logger.Error(_serverLocalisationService.GetText("quest-no_skill_found", skill));
            return false;
        }

        return profileSkill.Progress >= 5100; // 51
    }

    /// <summary>
    ///     Add points to a specific skill in player profile
    /// </summary>
    /// <param name="pmcProfile">Player profile with skill</param>
    /// <param name="skill">Skill to add points to</param>
    /// <param name="pointsToAddToSkill">Points to add</param>
    /// <param name="useSkillProgressRateMultiplier">Skills are multiplied by a value in globals, default is off to maintain compatibility with legacy code</param>
    public void AddSkillPointsToPlayer(
        PmcData pmcProfile,
        SkillTypes skill,
        double? pointsToAddToSkill,
        bool useSkillProgressRateMultiplier = false
    )
    {
        if (pointsToAddToSkill < 0D)
        {
            _logger.Warning(
                _serverLocalisationService.GetText(
                    "player-attempt_to_increment_skill_with_negative_value",
                    skill
                )
            );
            return;
        }

        var profileSkills = pmcProfile?.Skills?.Common;
        if (profileSkills == null)
        {
            _logger.Warning(
                $"Unable to add: {pointsToAddToSkill} points to {skill}, Profile has no skills"
            );
            return;
        }

        var profileSkill = profileSkills.FirstOrDefault(s => s.Id == skill);
        if (profileSkill == null)
        {
            _logger.Error(_serverLocalisationService.GetText("quest-no_skill_found", skill));
            return;
        }

        if (useSkillProgressRateMultiplier)
        {
            var skillProgressRate = _databaseService
                .GetGlobals()
                .Configuration.SkillsSettings.SkillProgressRate;
            pointsToAddToSkill *= skillProgressRate;
        }

        if (_inventoryConfig.SkillGainMultipliers.TryGetValue(skill.ToString(), out _))
        {
            pointsToAddToSkill *= _inventoryConfig.SkillGainMultipliers[skill.ToString()];
        }

        profileSkill.Progress += pointsToAddToSkill;
        profileSkill.Progress = Math.Min(profileSkill?.Progress ?? 0D, 5100); // Prevent skill from ever going above level 51 (5100)

        profileSkill.PointsEarnedDuringSession ??= 0;
        profileSkill.PointsEarnedDuringSession += pointsToAddToSkill;

        profileSkill.LastAccess = _timeUtil.GetTimeStamp();
    }

    /// <summary>
    ///     Is the provided session id for a developer account
    /// </summary>
    /// <param name="sessionID">Profile id to check</param>
    /// <returns>True if account is developer</returns>
    public bool IsDeveloperAccount(string sessionID)
    {
        return GetFullProfile(sessionID)
                ?.ProfileInfo?.Edition?.ToLowerInvariant()
                .StartsWith("spt developer") ?? false;
    }

    /// <summary>
    ///     Add stash row bonus to profile or increments rows given count if it already exists
    /// </summary>
    /// <param name="sessionId">Profile id to give rows to</param>
    /// <param name="rowsToAdd">How many rows to give profile</param>
    public void AddStashRowsBonusToProfile(string sessionId, int rowsToAdd)
    {
        var profile = GetPmcProfile(sessionId);
        if (profile?.Bonuses is null)
        {
            // Something is very wrong with profile to lack bonuses array, likely broken profile, exit early
            return;
        }
        var existingBonus = profile?.Bonuses.FirstOrDefault(b => b.Type == BonusType.StashRows);
        if (existingBonus is null)
        {
            profile!.Bonuses.Add(
                new Bonus
                {
                    Id = new MongoId(),
                    Value = rowsToAdd,
                    Type = BonusType.StashRows,
                    IsPassive = true,
                    IsVisible = true,
                    IsProduction = false,
                }
            );
        }
        else
        {
            existingBonus.Value += rowsToAdd;
        }
    }

    public bool HasAccessToRepeatableFreeRefreshSystem(PmcData pmcProfile)
    {
        return _gameEditionsWithFreeRefresh.Contains(pmcProfile.Info.GameVersion);
    }

    /// <summary>
    ///     Find a profiles "Pockets" item and replace its tpl with passed in value
    /// </summary>
    /// <param name="pmcProfile">Player profile</param>
    /// <param name="newPocketTpl">New tpl to set profiles Pockets to</param>
    public void ReplaceProfilePocketTpl(PmcData pmcProfile, string newPocketTpl)
    {
        // Find all pockets in profile, may be multiple as they could have equipment stand
        // (1 pocket for each upgrade level of equipment stand)
        var pockets = pmcProfile.Inventory.Items.Where(i => i.SlotId == "Pockets");
        if (!pockets.Any())
        {
            _logger.Error(
                $"Unable to replace profile: {pmcProfile.Id} pocket tpl with: {newPocketTpl} as Pocket item could not be found."
            );
            return;
        }

        foreach (var pocket in pockets)
        {
            pocket.Template = newPocketTpl;
        }
    }

    /// <summary>
    ///     Return a favorites list in the format expected by the GetOtherProfile call
    /// </summary>
    /// <param name="profile"></param>
    /// <returns>A list of Item objects representing the favorited data</returns>
    public List<Item> GetOtherProfileFavorites(PmcData profile)
    {
        var fullFavorites = new List<Item>();

        foreach (var itemId in profile.Inventory?.FavoriteItems ?? [])
        {
            // When viewing another users profile, the client expects a full item with children, so get that
            var itemAndChildren = profile.Inventory.Items.FindAndReturnChildrenAsItems(itemId);
            if (itemAndChildren?.Count > 0)
            {
                // To get the client to actually see the items, we set the main item's parent to null, so it's treated as a root item
                var clonedItems = _cloner.Clone(itemAndChildren);
                clonedItems.First().ParentId = null;

                fullFavorites.AddRange(clonedItems);
            }
        }

        return fullFavorites;
    }

    public void AddHideoutCustomisationUnlock(SptProfile fullProfile, Reward reward, string source)
    {
        if (fullProfile?.CustomisationUnlocks == null)
        {
            fullProfile.CustomisationUnlocks = [];
        }

        if (fullProfile?.CustomisationUnlocks?.Any(u => u.Id == reward.Target) ?? false)
        {
            _logger.Warning(
                $"Profile: {fullProfile.ProfileInfo.ProfileId} already has hideout customisation reward: {reward.Target}, skipping"
            );
            return;
        }

        var customisationTemplateDb = _databaseService.GetTemplates().Customization;
        var matchingCustomisation = customisationTemplateDb.GetValueOrDefault(reward.Target, null);

        if (matchingCustomisation is not null)
        {
            var rewardToStore = new CustomisationStorage
            {
                Id = reward.Target,
                Source = source,
                Type = null,
            };

            switch (matchingCustomisation.Parent)
            {
                case CustomisationTypeId.MANNEQUIN_POSE:
                    rewardToStore.Type = CustomisationType.MANNEQUIN_POSE;
                    break;
                case CustomisationTypeId.GESTURES:
                    rewardToStore.Type = CustomisationType.GESTURE;
                    break;
                case CustomisationTypeId.FLOOR:
                    rewardToStore.Type = CustomisationType.FLOOR;
                    break;
                case CustomisationTypeId.DOG_TAGS:
                    rewardToStore.Type = CustomisationType.DOG_TAG;
                    break;
                case CustomisationTypeId.CEILING:
                    rewardToStore.Type = CustomisationType.CEILING;
                    break;
                case CustomisationTypeId.WALL:
                    rewardToStore.Type = CustomisationType.WALL;
                    break;
                case CustomisationTypeId.ENVIRONMENT_UI:
                    rewardToStore.Type = CustomisationType.ENVIRONMENT;
                    break;
                case CustomisationTypeId.SHOOTING_RANGE_MARK:
                    rewardToStore.Type = CustomisationType.SHOOTING_RANGE_MARK;
                    break;
                default:
                    _logger.Error(
                        $"Unhandled customisation unlock type: {matchingCustomisation.Parent} not added to profile"
                    );
                    return;
            }

            fullProfile.CustomisationUnlocks.Add(rewardToStore);
        }
    }

    /// <summary>
    /// Get a profile template by the account and side
    /// </summary>
    /// <param name="accountEdition">Edition of profile desired, e.g. "Standard"</param>
    /// <param name="side">Side of profile desired, e.g. "Bear"</param>
    /// <returns></returns>
    public TemplateSide GetProfileTemplateForSide(string accountEdition, string side)
    {
        var profileTemplates = _databaseService.GetProfileTemplates();

        // Get matching profile 'type' e.g. 'standard'
        profileTemplates.TryGetValue(accountEdition, out var matchingProfileTemplate);

        // Get matching profile by 'side' e.g. USEC
        return string.Equals(side, "bear", StringComparison.OrdinalIgnoreCase)
            ? matchingProfileTemplate.Bear
            : matchingProfileTemplate.Usec;
    }
}
