﻿using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using DirigoEdge.Areas.Admin.Models;
using DirigoEdge.Areas.Admin.Models.ViewModels;
using DirigoEdge.Controllers;
using DirigoEdgeCore.Controllers;
using DirigoEdgeCore.Data.Entities;
using DirigoEdgeCore.Utils;

namespace DirigoEdge.Areas.Admin.Controllers
{
    public class NavigationController : DirigoBaseAdminController
    {
        [PermissionsFilter(Permissions = "Can Edit Navigation")]
        public ActionResult ManageNavigation()
        {
            var model = new ManageNavigationViewModel();
            return View(model);
        }

        [PermissionsFilter(Permissions = "Can Edit Navigation")]
        public ActionResult AddNavigation()
        {
            // Create a new Content Page to be passed to the edit content action
            var nav = new Navigation() { Name = "Temp" };

            // Add the nav
            Context.Navigations.Add(nav);
            Context.SaveChanges();

            // Add a top level item to get started
            var navItem = new NavigationItem()
            {
                Name = "SubNav Item",
                ParentNavigationId = nav.NavigationId
            };

            Context.NavigationItems.Add(navItem);
            Context.SaveChanges();

            // Update the Navigation Name with the new id we now have
            nav.Name = "Navigation " + nav.NavigationId;
            Context.SaveChanges();

            return RedirectToAction("EditNav", "Navigation", new { id = nav.NavigationId });
        }

        [PermissionsFilter(Permissions = "Can Edit Navigation")]
        public ActionResult EditNav(int id)
        {
            // Create a page and return the view
            var model = new EditNavigationViewModel(id);
            return View(model);
        }

        [PermissionsFilter(Permissions = "Can Edit Navigation")]
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult GetNewNavItem(int parentId)
        {
            var result = new JsonResult()
            {
                Data = new
                {
                    success = false,
                    message = "There was an error processing your request."
                }
            };
            string html = "";
            var success = 0;

            var navItem = new NavigationItem { ParentNavigationId = parentId, Name = "Menu Item" };
            Context.NavigationItems.Add(navItem);
            success = Context.SaveChanges();

            var container = new ParentNavViewContainer { NavItem = navItem, NavViewModel = new EditNavigationViewModel(parentId) { } };

            html = ContentUtils.RenderPartialViewToString("/Areas/Admin/Views/Navigation/ParentNavItemPartial.cshtml", container, ControllerContext, ViewData, TempData);

            result.Data = new { html };

            if (success > 0)
            {
                result.Data = new
                {
                    success = true,
                    message = "Retrieved new nav item",
                    html = html
                };
            }
            return result;
        }

        [PermissionsFilter(Permissions = "Can Edit Navigation")]
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult removeNavItem(int id)
        {
            var result = new JsonResult()
            {
                Data = new
                {
                    success = false,
                    message = "There was an error processing your request."
                }
            };
            var success = 0;

            var navItem = Context.NavigationItems.FirstOrDefault(x => x.NavigationItemId == id);
            Context.NavigationItems.Remove(navItem);
            success = Context.SaveChanges();

            if (success > 0)
            {
                result.Data = new
                {
                    success = true,
                    message = "Removed nav item."
                };
            }
            return result;
        }

        [PermissionsFilter(Permissions = "Can Edit Navigation")]
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult SaveNavigationSet(int navigationId, string name, List<NavigationItem> items)
        {
            var result = new JsonResult()
            {
                Data = new
                {
                    success = false,
                    message = "There was an error processing your request."
                }
            };
            var success = 0;
            // Update the nav name
            var nav = Context.Navigations.FirstOrDefault(x => x.NavigationId == navigationId);
            nav.Name = name;

            // Make sure Nav Items aren't null
            items = items ?? new List<NavigationItem>();

            // Update the navigation children
            foreach (var navItem in items)
            {
                var item = Context.NavigationItems.FirstOrDefault(x => x.NavigationItemId == navItem.NavigationItemId);
                if (item != null)
                {
                    item.Name = navItem.Name;
                    item.Href = navItem.Href;
                    item.Order = navItem.Order;
                    item.ParentNavigationId = navItem.ParentNavigationId;
                    item.ParentNavigationItemId = navItem.ParentNavigationItemId;
                    item.UsesContentPage = navItem.UsesContentPage;
                    item.ContentPageId = navItem.ContentPageId;
                    item.TargetBlank = navItem.TargetBlank;
                    //item.Promo = navItem.Promo;
                }
            }

            success = Context.SaveChanges();

            if (success > 0)
            {
                BookmarkUtil.DeleteBookmarkForUrl("/admin/navigation/editnav/" + navigationId +"/");
                // Clear the cache of the nav on save
                CachedObjects.GetMasterNavigationList(Context, true);
                CachedObjects.GetCacheNavigationList(true);

                result.Data = new
                {
                    success = true,
                    message = "Saved navigation successfully."
                };
            }

            return result;
        }

        [HttpPost]
        [PermissionsFilter(Permissions = "Can Edit Pages")]
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult GetPageUrl(int pageId, int categoryId)
        {
            var result = new JsonResult()
            {
                Data = new
                {
                    success = false,
                    message = "There was an error processing your request."
                }
            };
            string href = "";
            var success = 0;

            var page = Context.ContentPages.FirstOrDefault(x => x.ContentPageId == pageId);
            if (page != null)
            {
                success = 1;
                page.ParentNavigationItemId = categoryId;
                href = NavigationUtils.GetGeneratedUrl(page);
            }

            result.Data = new { url = href };

            if (success > 0)
            {
                result.Data = new
                {
                    success = true,
                    message = "Retrieved page url.",
                    url = href
                };
            }

            return result;
        }

    }
}
