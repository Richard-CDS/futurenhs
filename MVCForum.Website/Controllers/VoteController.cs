﻿namespace MvcForum.Web.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web.Mvc;
    using System.Web.Security;
    using Core.ExtensionMethods;
    using Core.Interfaces;
    using Core.Interfaces.Services;
    using Core.Models;
    using Core.Models.Entities;
    using Core.Models.Enums;
    using ViewModels;
    using ViewModels.Vote;
    using MembershipUser = Core.Models.Entities.MembershipUser;

    [Authorize]
    public partial class VoteController : BaseController
    {
        private readonly IMembershipUserPointsService _membershipUserPointsService;
        private readonly IPostService _postService;
        private readonly ITopicService _topicService;
        private readonly IVoteService _voteService;

        public VoteController(ILoggingService loggingService, IMembershipService membershipService,
            ILocalizationService localizationService, IRoleService roleService, IPostService postService,
            IVoteService voteService, ISettingsService settingsService, ITopicService topicService,
            IMembershipUserPointsService membershipUserPointsService, ICacheService cacheService,
            IMvcForumContext context)
            : base(loggingService, membershipService, localizationService, roleService,
                settingsService, cacheService, context)
        {
            _postService = postService;
            _voteService = voteService;
            _topicService = topicService;
            _membershipUserPointsService = membershipUserPointsService;
        }

        [HttpPost]
        [ActionName("VoteUpPost")]
        [AsyncTimeout(30000)]
        [HandleError(ExceptionType = typeof(TimeoutException), View = "TimeoutError")]
        public virtual async Task<ActionResult> VoteUpPostAsync(EntityIdViewModel voteUpViewModel)
        {
            if (Request.IsAjaxRequest())
            {
                User.GetMembershipUser(MembershipService);

                // Quick check to see if user is locked out, when logged in
                if (LoggedOnReadOnlyUser.IsLockedOut | !LoggedOnReadOnlyUser.IsApproved)
                {
                    FormsAuthentication.SignOut();
                    throw new Exception(LocalizationService.GetResourceString("Errors.NoAccess"));
                }

                // Get a db user
                var loggedOnUser = MembershipService.GetUser(LoggedOnReadOnlyUser?.Id);

                // Firstly get the post
                var post = _postService.Get(voteUpViewModel.Id);

                // Now get the current user
                var voter = loggedOnUser;

                // Also get the user that wrote the post
                var postWriter = post.User;

                // Mark the post up or down
                _ = await MarkPostUpOrDownAsync(post, postWriter, voter, PostType.Positive, LoggedOnReadOnlyUser);

                try
                {
                    Context.SaveChanges();
                }

                catch (Exception ex)
                {
                    Context.RollBack();
                    LoggingService.Error(ex);
                    throw new Exception(LocalizationService.GetResourceString("Errors.GenericMessage"));
                }
            }

            // TODO - need to return something more meaningful
            return Content(string.Empty);
        }

        [HttpPost]
        [ActionName("VoteDownPost")]
        [HandleError(ExceptionType = typeof(TimeoutException), View = "TimeoutError")]
        [AsyncTimeout(30000)]
        public virtual async Task<ActionResult> VoteDownPostAsync(EntityIdViewModel voteDownViewModel)
        {
            if (Request.IsAjaxRequest())
            {
                User.GetMembershipUser(MembershipService);

                // Quick check to see if user is locked out, when logged in
                if (LoggedOnReadOnlyUser.IsLockedOut | !LoggedOnReadOnlyUser.IsApproved)
                {
                    FormsAuthentication.SignOut();
                    throw new Exception(LocalizationService.GetResourceString("Errors.NoAccess"));
                }

                // Get a db user
                var loggedOnUser = MembershipService.GetUser(LoggedOnReadOnlyUser?.Id);

                // Firstly get the post
                var post = _postService.Get(voteDownViewModel.Id);

                // Now get the current user
                var voter = loggedOnUser;

                // Also get the user that wrote the post
                var postWriter = post.User;

                // Mark the post up or down
                _ = await MarkPostUpOrDownAsync(post, postWriter, voter, PostType.Negative, LoggedOnReadOnlyUser);

                try
                {
                    Context.SaveChanges();
                }
                catch (Exception ex)
                {
                    Context.RollBack();
                    LoggingService.Error(ex);
                    throw new Exception(LocalizationService.GetResourceString("Errors.GenericMessage"));
                }
            }

            // TODO - need to return something more meaningful
            return Content(string.Empty);
        }

        [ActionName("MarkPostUpOrDown")]
        [AsyncTimeout(30000)]
        [HandleError(ExceptionType = typeof(TimeoutException), View = "TimeoutError")]
        private async Task<bool> MarkPostUpOrDownAsync(Post post, MembershipUser postWriter, MembershipUser voter, PostType postType,
            MembershipUser LoggedOnReadOnlyUser)
        {
            var settings = SettingsService.GetSettings();
            // Check this user is not the post owner
            if (voter.Id != postWriter.Id)
            {
                // Not the same person, now check they haven't voted on this post before
                var votes = post.Votes.Where(x => x.VotedByMembershipUser.Id == LoggedOnReadOnlyUser?.Id).ToList();
                if (votes.Any())
                {
                    // Already voted, so delete the vote and remove the points
                    var votesToDelete = new List<Vote>();
                    votesToDelete.AddRange(votes);
                    foreach (var vote in votesToDelete)
                    {
                        _voteService.Delete(vote);
                    }

                    // Update the post with the new points amount
                    var newPointTotal = postType == PostType.Negative ? post.VoteCount + 1 : post.VoteCount - 1;
                    post.VoteCount = newPointTotal;
                }
                else
                {
                    // Points to add or subtract to a user
                    var usersPoints = postType == PostType.Negative
                        ? -settings.PointsDeductedNagativeVote
                        : settings.PointsAddedPostiveVote;

                    // Update the post with the new vote of the voter
                    var vote = new Vote
                    {
                        Post = post,
                        User = postWriter,
                        Amount = postType == PostType.Negative ? -1 : 1,
                        VotedByMembershipUser = voter,
                        DateVoted = DateTime.UtcNow
                    };
                    _voteService.Add(vote);

                    // Update the users points who wrote the post
                    await _membershipUserPointsService.Add(new MembershipUserPoints
                    {
                        Points = usersPoints,
                        User = postWriter,
                        PointsFor = PointsFor.Vote,
                        PointsForId = vote.Id
                    });

                    // Update the post with the new points amount
                    var newPointTotal = postType == PostType.Negative ? post.VoteCount - 1 : post.VoteCount + 1;
                    post.VoteCount = newPointTotal;
                }
            }

            return true;
        }

        [HttpPost]
        [AsyncTimeout(30000)]
        [HandleError(ExceptionType = typeof(TimeoutException), View = "TimeoutError")]
        [ActionName("MarkAsSolution")]
        public virtual async Task<ActionResult> MarkAsSolutionAsync(EntityIdViewModel markAsSolutionViewModel)
        {
            if (Request.IsAjaxRequest())
            {
                User.GetMembershipUser(MembershipService);

                // Quick check to see if user is locked out, when logged in
                if (LoggedOnReadOnlyUser.IsLockedOut | !LoggedOnReadOnlyUser.IsApproved)
                {
                    FormsAuthentication.SignOut();
                    throw new Exception(LocalizationService.GetResourceString("Errors.NoAccess"));
                }


                // Get a db user
                var loggedOnUser = MembershipService.GetUser(LoggedOnReadOnlyUser?.Id);

                // Firstly get the post
                var post = _postService.Get(markAsSolutionViewModel.Id);

                // Person who created the solution post
                var solutionWriter = post.User;

                // Get the post topic
                var topic = post.Topic;

                // Now get the current user
                var marker = loggedOnUser;
                try
                {
                    var solved = await _topicService.SolveTopic(topic, post, marker, solutionWriter);
                    if (solved)
                    {
                        Context.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    Context.RollBack();
                    LoggingService.Error(ex);
                    throw new Exception(LocalizationService.GetResourceString("Errors.GenericMessage"));
                }
            }

            // TODO - Should be returning something more meaningful
            return Content(string.Empty);
        }


        [HttpPost]
        public virtual PartialViewResult GetVoters(EntityIdViewModel voteUpViewModel)
        {
            if (Request.IsAjaxRequest())
            {
                var post = _postService.Get(voteUpViewModel.Id);
                var positiveVotes = post.Votes.Where(x => x.Amount > 0);
                var viewModel = new ShowVotersViewModel {Votes = positiveVotes.ToList()};
                return PartialView(viewModel);
            }
            return null;
        }

        [HttpPost]
        public virtual PartialViewResult GetVotes(EntityIdViewModel voteUpViewModel)
        {
            if (Request.IsAjaxRequest())
            {
                var post = _postService.Get(voteUpViewModel.Id);
                var positiveVotes = post.Votes.Count(x => x.Amount > 0);
                var negativeVotes = post.Votes.Count(x => x.Amount <= 0);
                var viewModel = new ShowVotesViewModel {DownVotes = negativeVotes, UpVotes = positiveVotes};
                return PartialView(viewModel);
            }
            return null;
        }

    }
}