using Elmah;
using ShortMe.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Configuration;
using System.Web.Mvc;

namespace ShortMe.Controllers
{
    public class HomeController : Controller
    {
        private readonly Entities db = new Entities();
        private readonly string SHORTTHEMUP = WebConfigurationManager.AppSettings["ShortThemUpUri"];
        private readonly string SN3R = WebConfigurationManager.AppSettings["Sn3rUri"];

        public RedirectResult Index(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return Redirect(SHORTTHEMUP);
            } 
            
            var r = new Regex("^[a-zA-Z0-9]*$");

            if (!r.IsMatch(id))
            {
                return Redirect(SHORTTHEMUP);
            }
            id = id.Trim();
            var shortUrl = SN3R + id;

            using (var dbContextTransaction = db.Database.BeginTransaction())
            {
                try
                {
                    var url = db.Urls.FirstOrDefault(u => u.ShortUrl == shortUrl);

                    if (url == null)
                    {
                        return Redirect(SHORTTHEMUP);
                    }
                    if ((url.MaxClicks != 0 && url.Clicks >= url.MaxClicks) || (url.Expires && url.HasExpired))
                    {
                        return Redirect(SHORTTHEMUP);
                    }
                    url.Clicks++;

                    db.Entry(url).State = EntityState.Modified;
                    db.SaveChanges();

                    var urlClick = new UrlClick();

                    do
                    {
                        urlClick.Id = Guid.NewGuid().ToString();
                    } while (db.UrlClicks.Find(urlClick.Id) != null);

                    urlClick.ClickedAt = DateTime.Now;
                    urlClick.UrlId = url.Id;
                    db.UrlClicks.Add(urlClick);
                    db.SaveChanges();
                    dbContextTransaction.Commit();
                    return Redirect(url.LongUrl);

                }
                catch (Exception ex)
                {
                    ErrorSignal.FromCurrentContext().Raise(ex);
                    dbContextTransaction.Rollback();
                }
            }
            return Redirect(SHORTTHEMUP);
        }
    }
}