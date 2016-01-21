using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using IDLake.Web;
using Newtonsoft.Json;

public partial class pages_publik_Authentication : System.Web.UI.Page
{
    private WebHub _hub;
    protected void Page_Load(object sender, EventArgs e)
    {
        var hubManager = new DefaultHubManager(GlobalHost.DependencyResolver);
        _hub = hubManager.ResolveHub("WebHub") as WebHub;
        string req = Request.QueryString["op"];
        Response.Clear();
        OutputCls status = new OutputCls();
        if (req == "signin")
        {
            string username = Request["username"];
            string password = Request["password"];
            var output = _hub.Login(username, password);

            Response.ContentType = "application/json; charset=utf-8";
            
            if (output.Result.Value)
            {
                FormsAuthentication.SetAuthCookie(username, false);
               
                //var test = System.Web.HttpContext.Current.User.Identity.IsAuthenticated;
                status.Result = true;
                Response.Write(Newtonsoft.Json.JsonConvert.SerializeObject(status));
            }
            else
            {
                status.Result = false;
                Response.Write(Newtonsoft.Json.JsonConvert.SerializeObject(status));
            }

        }
        else
        {
            status.Result = false;
            Response.ContentType = "application/json";
            HttpContext.Current.Session.Abandon();
            FormsAuthentication.SignOut();
            Response.Write(Newtonsoft.Json.JsonConvert.SerializeObject(status));
        }
        Response.End();
        
    }
}