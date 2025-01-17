﻿using System.ComponentModel.DataAnnotations.Schema;

namespace MvcForum.Core.Models.Entities
{
    using MvcForum.Core.ExtensionMethods;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Enums;
    using General;
    using Interfaces;
    using Utilities;

    /// <summary>
    ///     A membership user
    /// </summary>
    public partial class MembershipUser : ExtendedDataEntity, IBaseEntity
    {
        public MembershipUser()
        {
            Id = GuidComb.GenerateComb();

            // Default size of 100 for now, override with something else if required
            MemberImageSize = 100;
        }

        public Guid Id { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string Surname { get; set; }
        public string Initials { get; set; }
        public string Password { get; set; }
        public string PasswordSalt { get; set; }
        public string Email { get; set; }
        public string PasswordQuestion { get; set; }
        public string PasswordAnswer { get; set; }
        public bool IsApproved { get; set; }
        public bool IsLockedOut { get; set; }
        public bool IsBanned { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime LastLoginDate { get; set; }
        public DateTime LastPasswordChangedDate { get; set; }
        public DateTime LastLockoutDate { get; set; }
        public DateTime? LastActivityDate { get; set; }
        public int FailedPasswordAttemptCount { get; set; }
        public int FailedPasswordAnswerAttempt { get; set; }
        public string PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenCreatedAt { get; set; }
        public string Comment { get; set; }
        public string Slug { get; set; }
        public string Signature { get; set; }
        public int? Age { get; set; }
        public string Location { get; set; }
        public string Website { get; set; }
        public string Twitter { get; set; }
        public string Facebook { get; set; }

        public string Avatar { get; set; }
        public string FacebookAccessToken { get; set; }
        public long? FacebookId { get; set; }
        public string TwitterAccessToken { get; set; }
        public string TwitterId { get; set; }
        public string GoogleAccessToken { get; set; }
        public string GoogleId { get; set; }
        public string MicrosoftAccessToken { get; set; }
        public string MicrosoftId { get; set; }
        public bool? IsExternalAccount { get; set; }
        public bool? TwitterShowFeed { get; set; }
        public DateTime? LoginIdExpires { get; set; }
        public string MiscAccessToken { get; set; }

        public bool? DisableEmailNotifications { get; set; }
        public bool? DisablePosting { get; set; }
        public bool? DisablePrivateMessages { get; set; }
        public bool? DisableFileUploads { get; set; }

        public bool? HasAgreedToTermsAndConditions { get; set; }

        public bool IsTrustedUser { get; set; }

        public virtual IList<MembershipRole> Roles { get; set; }
        public virtual IList<Post> Posts { get; set; }
        public virtual IList<Topic> Topics { get; set; }
        public virtual IList<Vote> Votes { get; set; }
        public virtual IList<Vote> VotesGiven { get; set; }
        public virtual IList<GroupNotification> GroupNotifications { get; set; }
        public virtual IList<TopicNotification> TopicNotifications { get; set; }
        public virtual IList<TagNotification> TagNotifications { get; set; }
        public virtual IList<MembershipUserPoints> Points { get; set; }
        public virtual IList<Poll> Polls { get; set; }
        public virtual IList<PollVote> PollVotes { get; set; }
        public virtual IList<Favourite> Favourites { get; set; }
        public virtual IList<UploadedFile> UploadedFiles { get; set; }
        public virtual IList<Block> BlockedUsers { get; set; }
        public virtual IList<Block> BlockedByOtherUsers { get; set; }
        public virtual IList<PostEdit> PostEdits { get; set; }

        public int TotalPoints
        {
            get { return Points?.Select(x => x.Points).Sum() ?? 0; }
        }

        public string GetFullName()
        {
            if (!String.IsNullOrEmpty(this.FirstName) && !String.IsNullOrEmpty(this.Surname))
            {
                return String.Format("{0} {1}", this.FirstName, this.Surname).CapitaliseEachWord();
            }

            return this.UserName;
        }

        public string NiceUrl => UrlTypes.GenerateUrl(UrlType.Member, Slug);

        [NotMapped]
        public string CustomClassName { get; set; }
        [NotMapped]
        public int MemberImageSize { get; set; }
    }
}