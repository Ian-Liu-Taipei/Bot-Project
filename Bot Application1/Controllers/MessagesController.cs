﻿using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Web.Services.Description;
using System.Linq;
using System;
using System.Collections.Generic;
using BotCampDemo.Model;
using Microsoft.ProjectOxford.Vision;
using Microsoft.Cognitive.LUIS;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Vision.Contract;
using Newtonsoft.Json.Linq;
using Microsoft.ProjectOxford.Face.Contract;
using System.Threading;
using System.Data.SqlClient;
using System.Text;
using System.Data;

namespace Bot_Application1
{
    public class Global

    {
        public static string userid;
        public static Guid P_id;
        public static string upload_name;
    }
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>

        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {

                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                Activity reply = activity.CreateReply();
                //Trace.TraceInformation(JsonConvert.SerializeObject(reply, Formatting.Indented));

                if (activity.Attachments?.Count > 0 && activity.Attachments.First().ContentType.StartsWith("image"))
                {
                    //user傳送一張照片
                    ImageTemplate(reply, activity.Attachments.First().ContentUrl);

                }
                else if (activity.Text == "help_user")
                {
                    reply.Text = 
                        "1.新增使用者\n\n請輸入「新增名稱+『欲使用名稱』」\n\n" +
                        "例如:新增名稱王大明\n\n" +
                        "2.為使用者新增照片\n\n請輸入" +
                        "「上傳照片+『使用者名稱』」\n\n" +
                        "例如:上傳照片王大明\n\n" +
                        "若出現請上傳照片，即可傳圖片上傳\n\n" +
                        "3.刪除使用者\n\n請輸入「刪除使用者+『使用者名稱』」\n\n" +
                        "例如:刪除使用者王大明\n\n" +
                        "注意:刪除功能為不可逆功能。";
                }
                else if (activity.Text == "help_res")
                {
                    reply.Text =
                        "1.如需預約，請輸入\n\n「名稱+@+開始時間+@+結束時間」\n\n" +
                        "例如:名稱@王大明@2017/12/25-9:00@2017/12/25-10:00\n\n" +
                        "注意:時間格式請以yy/mm/dd-HH:MM輸入\n\n預約時間亦不可超過五小時\n\n" +
                        "如需超過五小時請分段預約\n\n" +
                        "2.如需取消預約，請輸入\n\n" +
                        "「刪除預約+名稱」\n\n" +
                        "例如:刪除預約王大明\n\n";
                        
                }
                else if (activity.Text == "help_search")
                {
                    reply.Text =
                        "如需查詢使用紀錄\n\n" +
                        "可使用此漢堡選單之快速查詢功能\n\n" +
                        "如需查詢特定使用者使用紀錄\n\n請輸入" +
                        "「查詢+本日/本周/本月+指定使用者+『欲查詢名稱』」\n\n" +
                        "例如:查詢本日指定使用者王大明\n\n";
                }
                else if (activity.Text == "subscription")
                {
                    string ChanData = activity.ChannelData.ToString();
                    dynamic json = JValue.Parse(ChanData);
                    SQL_Fb_id(json.sender.id,reply);
                }
                else if (activity.Text == "last")
                {
                    SQLCollectTimeOne(reply);
                }
                else if (activity.Text == "RECENT_TODAY_PAYLOAD")
                {
                    string timefinish = DateTime.UtcNow.AddHours(32).ToShortDateString();
                    string timestart = DateTime.UtcNow.AddHours(8).ToShortDateString();
                    SQLCollectTime(timestart, timefinish, reply);
                    
                }
                else if (activity.Text == "RECENT_WEEK_PAYLOAD")
                {
                    string timefinish = DateTime.UtcNow.AddHours(32).ToShortDateString();
                    string timestart = DateTime.UtcNow.AddDays(-7).ToShortDateString();
                    SQLCollectTime(timestart, timefinish, reply);
                }
                else if (activity.Text.StartsWith("delete"))
                {
                    string searchid = activity.Text.Trim("delete".ToCharArray());
                    string []strs = searchid.Split(new string[] { "~" }, StringSplitOptions.RemoveEmptyEntries);
                    DateTime dateValue_start;
                    DateTime dateValue_finish;
                    if (strs.Length == 2)
                    {
                        if (DateTime.TryParse(strs[0], out dateValue_start))
                        {
                            if (DateTime.TryParse(strs[1], out dateValue_finish))
                            {
                                SQLReserveTimeDelete(dateValue_start.ToString(),reply);
                            }
                        }
                    }
                }
                else
                {
                    //if(activity.ChannelId == "emulator")
                    if (activity.ChannelId == "facebook")
                    {
                        string nametest = activity.Text;
                        bool delete_user = nametest.StartsWith("刪除使用者");
                        bool keyin = nametest.StartsWith("新增名稱");
                        bool upload = nametest.StartsWith("上傳照片");
                        bool Test = nametest.StartsWith("測試");
                        bool res_time = nametest.StartsWith("預約");
                        bool res_delete = nametest.StartsWith("刪除預約");                    
                        bool recent_day = nametest.StartsWith("查詢本日指定使用者");
                        bool recent_week = nametest.StartsWith("查詢本周指定使用者");
                        bool recent_month = nametest.StartsWith("查詢本月指定使用者");
                        StateClient stateClient = activity.GetStateClient();
                        var fbData = JsonConvert.DeserializeObject<FBChannelModel>(activity.ChannelData.ToString());
                        if (fbData.postback != null)
                        {

                            var url = fbData.postback.payload.Split('>')[1];

                            if (fbData.postback.payload.StartsWith("Face>"))
                            {
                                try
                                {
                                    FaceServiceClient client = new FaceServiceClient("6ef41877566d45d68b93b527f187fbfa", "https://westcentralus.api.cognitive.microsoft.com/face/v1.0");
                                    Guid result_Person = Global.P_id;
                                    AddPersistedFaceResult result_add = await client.AddPersonFaceAsync("security", result_Person, url);
                                    await client.TrainPersonGroupAsync("security");
                                    TrainingStatus result = await client.GetPersonGroupTrainingStatusAsync("security");
                                    reply.Text = $"使用者照片已綁定";
                                   
                                }
                                catch (FaceAPIException f)
                                {
                                    reply.Text = "照片無法辨識，請重新上傳臉部清晰照片";
                                }
                                //faceAPI
                            }
                            else if (fbData.postback.payload.StartsWith("TypeIn"))
                            {


                            }
                            else
                                reply.Text = $"無法辨識的指令，可使用下側選單選取幫助查詢指令";
                        }
                        else if (keyin)
                        {
                            Global.userid = activity.Text.Trim("新增名稱".ToCharArray()); //移除"名稱"
                            if (!SQLUserNameCheck(Global.userid))
                            {
                                try
                                {
                                    FaceServiceClient client = new FaceServiceClient("6ef41877566d45d68b93b527f187fbfa", "https://westcentralus.api.cognitive.microsoft.com/face/v1.0");
                                    CreatePersonResult result_Person = await client.CreatePersonAsync("security", Global.userid);
                                    Global.P_id = result_Person.PersonId;
                                    SQLNameRegister(Global.userid, result_Person.PersonId.ToString(), reply);
                                    reply.Text = $"user create as:{Global.userid}";
                                }
                                catch (FaceAPIException f)
                                {

                                    reply.Text = "" + f.ErrorMessage + "";
                                }
                            }
                            else
                            {
                                reply.Text = "name has been registry,select another name.";
                            }

                        }
                        else if (delete_user)
                        {

                            string username = activity.Text.Trim("刪除使用者".ToCharArray());
                            Guid PersonID = new Guid(SQLSelectId(username));
                            if (SQLSelectId(username) != "error")
                            {
                                FaceServiceClient client = new FaceServiceClient("6ef41877566d45d68b93b527f187fbfa", "https://westcentralus.api.cognitive.microsoft.com/face/v1.0");
                                try
                                {
                                    await client.DeletePersonAsync("security", PersonID);
                                    SQLDeleteRes(username);
                                    SQLPersonDelete(PersonID.ToString(), reply);
                                }
                                catch (FaceAPIException f)
                                {
                                    reply.Text = "delete failed," + f.ErrorMessage + "";
                                }
                            }
                            else
                            {
                                reply.Text = "can't find user";
                            }

                            //SQLDELETENAME select  personid where username == username

                        }
                        //else if (Test)
                        //{
                        //    string ChanData = activity.ChannelData.ToString();
                        //    dynamic json = JValue.Parse(ChanData);
                        //    reply.Text = json.sender.id;
                        //}
                        else if (res_time)
                        {
                            string searchid = activity.Text.Trim("預約".ToCharArray());
                            string[] strs = { };
                            DateTime dateValue_start;
                            DateTime dateValue_finish;
                            strs = searchid.Split(new string[] { "@" }, StringSplitOptions.None);
                            System.Text.StringBuilder sb = new System.Text.StringBuilder();
                            if (strs.Length == 3)
                            {
                                if (DateTime.TryParse(strs[1], out dateValue_start))
                                {
                                    if (DateTime.TryParse(strs[2], out dateValue_finish))
                                    {
                                        double s = new TimeSpan(dateValue_finish.Ticks - dateValue_start.Ticks).TotalMinutes;
                                        if ((dateValue_finish > dateValue_start) && (s <= 300))
                                        {
                                            if (SQLUserNameCheck(strs[0]))
                                            {
                                                if (!SQLReserveTimeIsConflict(dateValue_start, dateValue_finish, strs[0]))
                                                {
                                                    SQLReserveTimeInsert(dateValue_start, dateValue_finish, strs[0], reply);

                                                }
                                                else
                                                {
                                                    reply.Text = "指定時間已被預約，請重新選擇。";
                                                }
                                            }
                                            else
                                            {
                                                reply.Text = "使用者不在資料庫中，請先註冊再做預約。";
                                            }
                                        }
                                        else
                                        {
                                            reply.Text = "開始時間晚於結束時間或輸入時間大於5小時，請重新輸入。";
                                        }

                                    }
                                    else
                                    {
                                        reply.Text = ("日期格式錯誤，請參照範例格式。");
                                    }
                                }
                                else
                                {
                                    reply.Text = ("日期格式錯誤，請參照範例格式。");
                                }
                            }
                            else
                            {
                                reply.Text = "請輸入格式:名稱@起始時間@結束時間";
                            }
                        }
                        else if (res_delete)
                        {
                            string searchid = activity.Text.Trim("刪除預約".ToCharArray());
                            //reply.Text = SQLReserveTimeDelete(searchid);
                            CreateButtonOne(reply, searchid);
                        }
                        else if (recent_day)
                        {
                            string searchid = activity.Text.Trim("查詢本日指定使用者".ToCharArray());
                            string timefinish = DateTime.UtcNow.AddHours(32).ToShortDateString();
                            string timestart = DateTime.UtcNow.AddHours(8).ToShortDateString();
                            SQLCollectTimeName(timestart, timefinish, searchid, reply);
                            //得到最近的時間
                        }
                        else if (recent_week)
                        {
                            string searchid = activity.Text.Trim("查詢本周指定使用者".ToCharArray());
                            string timefinish = DateTime.UtcNow.AddHours(32).ToShortDateString();
                            string timestart = DateTime.UtcNow.AddDays(-7).ToShortDateString();
                            SQLCollectTimeName(timestart, timefinish, searchid, reply);
                            //得到最近的時間
                        }
                        else if (recent_month)
                        {
                            string searchid = activity.Text.Trim("查詢本月指定使用者".ToCharArray());
                            string timefinish = DateTime.UtcNow.AddHours(32).ToShortDateString();
                            string timestart = DateTime.UtcNow.AddDays(-30).ToShortDateString();
                            SQLCollectTimeName(timestart, timefinish, searchid, reply);
                            //得到最近的時間
                        }
                        else if (upload)
                        {
                            string uploadname = activity.Text.Trim("上傳照片".ToCharArray());
                            if(SQLSelectId(uploadname) != "error")
                            {
                                Guid guid = new Guid(SQLSelectId(uploadname));
                                Global.P_id = guid;
                                reply.Text = "請上傳照片";
                            }
                            else
                            {
                                reply.Text = "無使用者資料，請先註冊";
                            }
                            
                        }
                        else
                        {
                            reply.Text = $"nope";
                        }

                    }
                }
                await connector.Conversations.ReplyToActivityAsync(reply);
            }

            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }


        private void ImageTemplate(Activity reply, string url)
        {
            List<Attachment> att = new List<Attachment>();
            att.Add(new HeroCard()
            {
                Title = "Cognitive services",
                Subtitle = "Select from below",
                Images = new List<CardImage>() { new CardImage(url) },
                Buttons = new List<CardAction>()
                    {
                        new CardAction(ActionTypes.PostBack, "上傳使用者圖片", value: $"Face>{url}"),
                        //new CardAction(ActionTypes.PostBack, "辨識圖片", value: $"Analyze>{url}")
                    }
            }.ToAttachment());

            reply.Attachments = att;
        }
        
        private void CreateButtonOne(Activity reply,string name)
        {
            string del_time = SQLReserveTimeDeleteCheck(name);
            string[] strs = del_time.Split(new string[] { "@@" }, StringSplitOptions.RemoveEmptyEntries);
            //reply.Text = (strs[0]+strs[1]+strs[2]);
            if (strs[0] == "error" || String.IsNullOrEmpty(strs[0]))
            {
                reply.Text = "資料庫無此筆資料，請重新確認";
            }
            else
            {
                List<Attachment> att = new List<Attachment>();
                foreach (string s in strs)
                {
                    att.Add(new HeroCard()
                    {
                        Title = "請問要刪除\n\n" + s + "",
                        Subtitle = "超過5筆則僅顯示最近5筆",
                        Buttons = new List<CardAction>()
                        {
                            new CardAction(ActionTypes.PostBack,"是", value: "delete"+s+""),
                        }

                    }.ToAttachment());
                }
                reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                reply.Attachments = att;
                
            }
        }
        private void SQLCollectTime(string timestart, string timefinish, Activity reply)
        {
            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
                builder.DataSource = "mrlsql.database.windows.net";
                builder.UserID = "mrlsql";
                builder.Password = "MRL666@mrl";
                builder.InitialCatalog = "mrlsql";
                StringBuilder sqlresult = new StringBuilder();

                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    connection.Open();
                    StringBuilder sb = new StringBuilder();
                    sb.Append("SELECT * FROM [dbo].[detect] WHERE detecttime >= CONVERT(datetime,'");
                    sb.Append(timestart);
                    sb.Append("', 110) and detecttime <= CONVERT(datetime,'");
                    sb.Append(timefinish);
                    sb.Append("', 110) order by detecttime; ");
                    String sql = sb.ToString();

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                //Console.WriteLine("{0} {1}", reader.GetString(0), reader.GetString(1));
                                sqlresult.Append(reader.GetString(1));
                                sqlresult.Append(" ");
                                sqlresult.Append(reader.GetDateTime(2).ToString("yyyy-MM-dd HH:mm:ss"));
                                sqlresult.Append("\n\n");

                                reply.Text = sqlresult.ToString();
                            }
                        }
                    }
                    connection.Close();
                }
            }
            catch (SqlException e)
            {
                reply.Text = $"{ e.ToString()}";
            }

        }
        private void SQLCollectTimeOne(Activity reply)
        {
            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
                builder.DataSource = "mrlsql.database.windows.net";
                builder.UserID = "mrlsql";
                builder.Password = "MRL666@mrl";
                builder.InitialCatalog = "mrlsql";
                StringBuilder sqlresult = new StringBuilder();
                //string time = activity.Text.Trim("測試".ToCharArray());

                //string timestart = "2017-09-03 12:10", timefinish = "2017-09-03 12:30"; //yyyy-mm-dd h-m-s


                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    connection.Open();
                    StringBuilder sb = new StringBuilder();
                    sb.Append("SELECT TOP 1 [PersonID],[Person],[detecttime] FROM [dbo].[detect] ORDER BY detecttime DESC");
                    /*sb.Append("FROM [SalesLT].[ProductCategory] pc ");
                    sb.Append("JOIN [SalesLT].[Product] p ");
                    sb.Append("ON pc.productcategoryid = p.productcategoryid;");*/
                    String sql = sb.ToString();

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            //sqlresult.Append("出現時間: \n\n");
                            //sqlresult.Append(timestart);
                            //sqlresult.Append(" till ");
                            //sqlresult.Append(timefinish);
                            //sqlresult.Append("\n\n");

                            while (reader.Read())
                            {
                                //Console.WriteLine("{0} {1}", reader.GetString(0), reader.GetString(1));
                                sqlresult.Append(reader.GetString(1));
                                sqlresult.Append(" ");
                                sqlresult.Append(reader.GetDateTime(2).ToString("yyyy-MM-dd HH:mm:ss"));
                                sqlresult.Append("\n\n");

                                reply.Text = sqlresult.ToString();
                            }
                        }
                    }
                    connection.Close();
                }
            }
            catch (SqlException e)
            {
                reply.Text = $"{ e.ToString()}";
            }



        }
        private void SQLCollectTimeName(string timestart, string timefinish,string name, Activity reply)
        {
            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
                builder.DataSource = "mrlsql.database.windows.net";
                builder.UserID = "mrlsql";
                builder.Password = "MRL666@mrl";
                builder.InitialCatalog = "mrlsql";
                StringBuilder sqlresult = new StringBuilder();                
                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    connection.Open();
                    StringBuilder sb = new StringBuilder();
                    sb.Append("SELECT * FROM [dbo].[detect] WHERE detecttime >= CONVERT(datetime,'");
                    sb.Append(timestart);
                    sb.Append("', 110) and detecttime <= CONVERT(datetime,'");
                    sb.Append(timefinish);
                    sb.Append("', 110) and Person = N'"+name+"' ORDER BY detecttime DESC ");
                    //sb.Append("SELECT * FROM [dbo].[detect] WHERE Person = '"+name+"' ORDER BY detecttime DESC");
                    /*sb.Append("FROM [SalesLT].[ProductCategory] pc ");
                    sb.Append("JOIN [SalesLT].[Product] p ");
                    sb.Append("ON pc.productcategoryid = p.productcategoryid;");*/
                    String sql = sb.ToString();

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            //sqlresult.Append("出現時間: \n\n");
                            //sqlresult.Append(timestart);
                            //sqlresult.Append(" till ");
                            //sqlresult.Append(timefinish);
                            //sqlresult.Append("\n\n");

                            while (reader.Read())
                            {
                                //Console.WriteLine("{0} {1}", reader.GetString(0), reader.GetString(1));
                                sqlresult.Append(reader.GetString(1));
                                sqlresult.Append(" ");
                                sqlresult.Append(reader.GetDateTime(2).ToString("yyyy-MM-dd HH:mm:ss"));
                                sqlresult.Append("\n\n");

                                reply.Text = sqlresult.ToString();
                            }
                        }
                    }
                    connection.Close();
                }
            }
            catch (SqlException e)
            {
                reply.Text = $"{ e.ToString()}";
            }
        }
        private bool SQLUserNameCheck(string name)
        {
            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
                builder.DataSource = "mrlsql.database.windows.net";
                builder.UserID = "mrlsql";
                builder.Password = "MRL666@mrl";
                builder.InitialCatalog = "mrlsql";
                StringBuilder sqlresult = new StringBuilder();
                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    connection.Open();
                    StringBuilder sb = new StringBuilder();
                    sb.Append("IF EXISTS(SELECT 1 FROM [dbo].[users] WHERE Name = N'"+name+"') BEGIN " +
                        "SELECT 'True' END ELSE BEGIN SELECT 'False' END");

                    String sql = sb.ToString();
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                sqlresult.Append(reader.GetString(0));
                            }
                            if (sqlresult.ToString() == "True")
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                            return false;
                        }
                    }
                    connection.Close();
                }
            }
            catch (SqlException e)
            {
                return false;
            }
        }
        private bool SQLReserveTimeIsConflict(DateTime timestart, DateTime timefinish, string name)
        {
            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
                builder.DataSource = "mrlsql.database.windows.net";
                builder.UserID = "mrlsql";
                builder.Password = "MRL666@mrl";
                builder.InitialCatalog = "mrlsql";
                StringBuilder sqlresult = new StringBuilder();
                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    connection.Open();
                    StringBuilder sb = new StringBuilder();
                    sb.Append("IF (SELECT COUNT(Person) FROM dbo.reservation WHERE StartTime <= CONVERT(datetime,'"+timefinish+"',110) and EndTime >= CONVERT(datetime,'"+timefinish+"',110)" +
                        " or StartTime <= CONVERT(datetime,'"+timestart+"',110) and EndTime >= CONVERT(datetime,'"+timestart+"',110)" +
                        " or StartTime >= CONVERT(datetime,'"+timestart+"',110) and EndTime <= CONVERT(datetime,'"+timefinish+"',110)) > 0 "+
                        " BEGIN SELECT 'True' END "+  
                        " ELSE BEGIN SELECT 'False' END");
                    String sql = sb.ToString();
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {                      
                                sqlresult.Append(reader.GetString(0));
                            }
                            if (sqlresult.ToString() == "True")
                            {
                                return true;
                                
                            }
                            else
                            {
                                return false;
                                
                            }
                            
                        }
                    }
                    connection.Close();
                    
                }
            }
            catch (SqlException e)
            {
                return false;
                //reply.Text = $"{ e.ToString()}";
            }



        }
        private void SQLReserveTimeInsert(DateTime timestart, DateTime timefinish, string name, Activity reply)
        {
            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
                builder.DataSource = "mrlsql.database.windows.net";
                builder.UserID = "mrlsql";
                builder.Password = "MRL666@mrl";
                builder.InitialCatalog = "mrlsql";
                StringBuilder sqlresult = new StringBuilder();
                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    connection.Open();
                    StringBuilder sb = new StringBuilder();
                    sb.Append("INSERT INTO [dbo].[reservation]([Person],[StartTime],[EndTime]) VALUES(N'"+ name +"', CONVERT(smalldatetime,'"+timestart+ "',110), CONVERT(smalldatetime,'"+timefinish+"',110))");
                    String sql = sb.ToString();
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                sqlresult.Append(reader.GetString(0));
                            }
                            reply.Text = "預約成功!請記得於預約時間使用!\n\n若無需使用請取消預約!";
                        }
                    }
                    connection.Close();
                }
            }
            catch (SqlException e)
            {
                reply.Text = "資料庫寫入失敗，請確認輸入格式正確後，稍後再試。";
                //reply.Text = $"{ e.ToString()}";
            }



        }
        private string SQLReserveTimeDeleteCheck(string name)
        {
            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
                builder.DataSource = "mrlsql.database.windows.net";
                builder.UserID = "mrlsql";
                builder.Password = "MRL666@mrl";
                builder.InitialCatalog = "mrlsql";
                StringBuilder sqlresult = new StringBuilder();
                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    connection.Open();
                    StringBuilder sb = new StringBuilder();
                    sb.Append("SELECT Top 5 * FROM [dbo].[reservation] WHERE Person = N'"+name+"' AND StartTime >= DateAdd(HH, 8, Getdate()) ORDER BY StartTime");
                    String sql = sb.ToString();
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                sqlresult.Append(reader.GetDateTime(1).ToString("yyyy-MM-dd HH:mm"));
                                sqlresult.Append("~");
                                sqlresult.Append(reader.GetDateTime(2).ToString("yyyy-MM-dd HH:mm"));
                                sqlresult.Append("@@");
                                //return sqlresult.ToString();                                
                            }
                            if (String.IsNullOrWhiteSpace(sqlresult.ToString()))
                            {
                                return "error";
                            }
                            else
                            {
                                return sqlresult.ToString();
                            }
                        }
                    }
                    connection.Close();
                }
            }
            catch (SqlException e)
            {
                return "error";
                //reply.Text = "資料庫無預約資料。";
                //reply.Text = $"{ e.ToString()}";
            }



        }
        private void SQLReserveTimeDelete(string StartTime,Activity reply)
        {
            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
                builder.DataSource = "mrlsql.database.windows.net";
                builder.UserID = "mrlsql";
                builder.Password = "MRL666@mrl";
                builder.InitialCatalog = "mrlsql";
                StringBuilder sqlresult = new StringBuilder();
                //string time = activity.Text.Trim("測試".ToCharArray());

                //string timestart = "2017-09-03 12:10", timefinish = "2017-09-03 12:30"; //yyyy-mm-dd h-m-s


                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    connection.Open();
                    StringBuilder sb = new StringBuilder();
                    sb.Append("DELETE FROM [dbo].[reservation] WHERE StartTime = '"+StartTime+"'");
                    String sql = sb.ToString();

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                           

                            while (reader.Read())
                            {
                                //Console.WriteLine("{0} {1}", reader.GetString(0), reader.GetString(1));
                                sqlresult.Append(reader.GetString(1));
                                sqlresult.Append(" ");
                                sqlresult.Append(reader.GetDateTime(2).ToString("yyyy-MM-dd HH:mm:ss"));
                                sqlresult.Append("\n\n");
                               
                            }
                            reply.Text = "Delete Sucess.";
                        }
                    }
                    connection.Close();
                }
            }
            catch (SqlException e)
            {
                reply.Text = $"{ e.ToString()}";
            }



        }
        private void SQLNameRegister(string name,string PersonID ,Activity reply)
        {
            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
                builder.DataSource = "mrlsql.database.windows.net";
                builder.UserID = "mrlsql";
                builder.Password = "MRL666@mrl";
                builder.InitialCatalog = "mrlsql";
                StringBuilder sqlresult = new StringBuilder();
                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    connection.Open();
                    StringBuilder sb = new StringBuilder();
                    sb.Append("INSERT INTO [dbo].[users]([NAME],[PersonId]) VALUES(N'"+name+"','"+PersonID+"')");
                    String sql = sb.ToString();
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                sqlresult.Append(reader.GetString(0));
                            }
                            reply.Text = "create person sucess";
                        }
                    }
                    connection.Close();
                }
            }
            catch (SqlException e)
            {
                reply.Text = "create person failed";
            }



        }
        private string SQLSelectId(string name)
        {
            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
                builder.DataSource = "mrlsql.database.windows.net";
                builder.UserID = "mrlsql";
                builder.Password = "MRL666@mrl";
                builder.InitialCatalog = "mrlsql";
                StringBuilder sqlresult = new StringBuilder();
                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    connection.Open();
                    StringBuilder sb = new StringBuilder();
                    sb.Append("IF EXISTS (SELECT Top 1 [PersonId] FROM [dbo].[users] WHERE Name = N'"+name+"')" +
                        "BEGIN SELECT Top 1 [PersonId] FROM [dbo].[users] WHERE Name = N'" + name + "' END ELSE BEGIN SELECT 'error' END");
                    String sql = sb.ToString();
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                sqlresult.Append(reader.GetString(0));

                            }
                            return sqlresult.ToString();
                        }
                    }
                    connection.Close();
                }
            }
            catch (SqlException e)
            {
                //reply.Text = $"{ e.ToString()}";
                return "error";
            }



        }
        private void SQLPersonDelete(string PersonID, Activity reply)
        {
            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
                builder.DataSource = "mrlsql.database.windows.net";
                builder.UserID = "mrlsql";
                builder.Password = "MRL666@mrl";
                builder.InitialCatalog = "mrlsql";
                StringBuilder sqlresult = new StringBuilder();
                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    connection.Open();
                    StringBuilder sb = new StringBuilder();
                    sb.Append("DELETE FROM [dbo].[users] WHERE PersonID = '"+PersonID+"'");
                    String sql = sb.ToString();
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                sqlresult.Append(reader.GetString(0));
                            }
                            reply.Text = "delete user sucess.";
                        }
                    }
                    connection.Close();
                }
            }
            catch (SqlException e)
            {
                reply.Text = "delete user failed";
                //reply.Text = $"{ e.ToString()}";
            }



        }
        private void SQLDeleteRes(string name)
        {
            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
                builder.DataSource = "mrlsql.database.windows.net";
                builder.UserID = "mrlsql";
                builder.Password = "MRL666@mrl";
                builder.InitialCatalog = "mrlsql";
                StringBuilder sqlresult = new StringBuilder();
                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    connection.Open();
                    StringBuilder sb = new StringBuilder();
                    sb.Append("DELETE FROM [dbo].[reservation] WHERE Person = N'" +name+ "'");
                    String sql = sb.ToString();
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                sqlresult.Append(reader.GetString(0));
                            }
                           
                        }
                    }
                    connection.Close();
                }
            }
            catch (SqlException e)
            {
                
                //reply.Text = $"{ e.ToString()}";
            }



        }
        private void SQL_Fb_id(dynamic fb_id, Activity reply)
        {
            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
                builder.DataSource = "mrlsql.database.windows.net";
                builder.UserID = "mrlsql";
                builder.Password = "MRL666@mrl";
                builder.InitialCatalog = "mrlsql";
                StringBuilder sqlresult = new StringBuilder();
                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    connection.Open();
                    StringBuilder sb = new StringBuilder();
                    sb.Append("IF NOT EXISTS(SELECT 1 FROM [dbo].[subscribe] WHERE fb_id = '"+fb_id+"')" +
                        "BEGIN INSERT INTO [dbo].[subscribe]([fb_id]) VALUES('"+fb_id+"') END");
                    String sql = sb.ToString();
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                sqlresult.Append(reader.GetString(0));
                            }
                            reply.Text = "訂閱成功";
                        }
                    }
                    connection.Close();
                }
            }
            catch (SqlException e)
            {
                reply.Text = "訂閱失敗，稍後再試。";
                //reply.Text = $"{ e.ToString()}";
            }



        }



    }
}