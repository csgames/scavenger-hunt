﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

using Microsoft.AspNet.Identity;

using ScavengerHunt.Web.Models;

using WebGrease.Css.Extensions;

namespace ScavengerHunt.Web.Controllers
{
    public class TeamStuntController : BaseController
    {
        private ScavengerHuntContext db = new ScavengerHuntContext();

        // GET: /TeamStunt/
        public ActionResult Index()
        {
            if (!User.Identity.IsAuthenticated) return RedirectToAction("Index", "Stunt");

            // Get current user
            string currentUserId = User.Identity.GetUserId();
            var user = db.Users.Find(currentUserId);

            if (user.Team == null)
            {
                return RedirectToAction("Index", "Stunt");
            }

            return View(db.TeamStunts.ToList().Globalize(Language));
        }

        public ActionResult ActivityPartial()
        {
            return PartialView(db.TeamStunts.ToList().Globalize(Language));
        }

        // GET: /TeamStunt/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            TeamStunt teamstunt = db.TeamStunts.Find(id);
            if (teamstunt == null) return HttpNotFound();

            // Make sure the current user is registered, part of a team and have access to this stunt
            var userid = User.Identity.GetUserId();
            var user = db.Users.Find(userid);

            if (user == null) return RedirectToAction("Login", "Account");
            if (user.Team == null) return RedirectToAction("Start", "Team");
            if (user.Team.TeamStunts.All(x => x.Id != id)) return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

            return View(teamstunt.Globalize(Language));
        }

        // POST: /TeamStunt/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include="Id,TeamNotes,Submission,Status")] TeamStunt teamstunt)
        {
            if (ModelState.IsValid)
            {
                // Get previous stunt object
                var teamStunt = db.TeamStunts.Find(teamstunt.Id);

                teamStunt.TeamNotes = teamstunt.TeamNotes;
                teamStunt.Submission = teamstunt.Submission;
                teamStunt.Status = teamstunt.Status;
                teamStunt.DateUpdated = DateTime.Now;

                // Special logic if it's a flag
                teamStunt = CorrectFlag(teamStunt);

                db.Entry(teamStunt).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(teamstunt);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private static TeamStunt CorrectFlag(TeamStunt teamStunt)
        {
            // Is the stunt of type flag?
            if (teamStunt.Stunt.Type != StuntTypeEnum.Flag) return teamStunt;

            // Is an answer provided for the flag?
            if (string.IsNullOrEmpty(teamStunt.Stunt.JudgeNotes)) return teamStunt;

            // Is it the right answer?
            if (teamStunt.Submission == teamStunt.Stunt.JudgeNotes)
            {
                teamStunt.Score = teamStunt.Stunt.MaxScore;
                teamStunt.Status = TeamStuntStatusEnum.Done;
                teamStunt.JudgeFeedback = "Nice flag!"; // TODO: Randomize those
            }
            else
            {
                teamStunt.Status = TeamStuntStatusEnum.WorkInProgress;
                teamStunt.JudgeFeedback = "Wrong flag";
            }

            return teamStunt;
        }
    }
}
