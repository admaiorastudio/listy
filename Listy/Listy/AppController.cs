namespace AdMaiora.Listy
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Threading;
    using System.Linq;

    using RestSharp.Portable;

    using AdMaiora.AppKit.Utils;
    using AdMaiora.AppKit.Data;
    using AdMaiora.AppKit.Services;
    using AdMaiora.AppKit.IO;

    using AdMaiora.Listy.Api;
    using AdMaiora.Listy.Model;
   
    public class PushEventArgs : EventArgs
    {
        public int Action
        {
            get;
            private set;
        }

        public string Payload
        {
            get;
            private set;
        }

        public Exception Error
        {
            get;
            private set;
        }

        public PushEventArgs(int action, string payload)
        {
            this.Action = action;
            this.Payload = payload;
        }

        public PushEventArgs(Exception error)
        {
            this.Error = error;
        }
    }

    public class TextInputDoneEventArgs : EventArgs
    {
        public TextInputDoneEventArgs(string text)
        {
            this.Text = text;
        }

        public string Text
        {
            get;
            private set;
        }
    }

    public class AppSettings
    {
        #region Constants and Fields

        private UserSettings _settings;

        #endregion

        #region Constructors

        public AppSettings(UserSettings settings)
        {
            _settings = settings;
        }

        #endregion

        #region Properties

        public string LastLoginUsernameUsed
        {
            get
            {
                return _settings.GetStringValue("LastLoginUsernameUsed");
            }
            set
            {
                _settings.SetStringValue("LastLoginUsernameUsed", value);
            }
        }

        public string AuthAccessToken
        {
            get
            {
                return _settings.GetStringValue("AuthAccesstoken");
            }
            set
            {
                _settings.SetStringValue("AuthAccesstoken", value);
            }
        }

        public DateTime? AuthExpirationDate
        {
            get
            {
                return _settings.GetDateTimeValue("AuthExpirationDate");
            }
            set
            {
                _settings.SetDateTimeValue("AuthExpirationDate", value);
            }
        }

        #endregion
    }

    public static class AppController
    {
        #region Inner Classes

        class TokenExpiredException : Exception
        {
            public TokenExpiredException()
                : base("Access Token is expired.")
            {

            }
        }

        #endregion

        #region Constants and Fields
        public static class Globals
        {
            // Splash screen timeout (milliseconds)
            public const int SplashScreenTimeout = 2000;

            // Data storage file uri
            public const string DatabaseFilePath = "internal://database.db3";

            // Base URL for service client endpoints
            public const string ServicesBaseUrl = "https://listy-api.azurewebsites.net/";            
            // Default service client timeout in seconds
            public const int ServicesDefaultRequestTimeout = 60;
        }

        public static class Colors
        {
            public const string MiddleRedPurple = "160C28";
            public const string OrangeYellow = "EFCB68";
            public const string Alabaster = "E1EFE6";
            public const string AshGray = "AEB7B3"; 
            public const string RichBlack = "000411";
            public const string Black = "000000";
            public const string White = "FFFFFF";
            public const string Green = "00A454";
            public const string Orange = "ED7218";
            public const string Red = "D01818";
        }

        private static AppSettings _settings;

        private static Executor _utility;
        private static FileSystem _filesystem;
        private static DataStorage _database;
        private static ServiceClient _services;        

        #endregion

        #region Properties

        public static AppSettings Settings
        {
            get
            {
                return _settings;
            }
        }

        public static Executor Utility
        {
            get
            {
                return _utility;
            }
        }

        public static ServiceClient Services
        {
            get
            {
                return _services;
            }

        }

        public static bool IsUserRestorable
        {
            get
            {
                if (String.IsNullOrWhiteSpace(AppController.Settings.AuthAccessToken))
                    return false;

                if (!(DateTime.Now < AppController.Settings.AuthExpirationDate.GetValueOrDefault()))
                    return false;

                return true;
            }
        }

        #endregion

        #region Initialization Methods

        public static void EnableSettings(IUserSettingsPlatform userSettingsPlatform)
        {
            _settings = new AppSettings(new UserSettings(userSettingsPlatform));
        }

        public static void EnableUtilities(IExecutorPlatform utilityPlatform)
        {
            _utility = new Executor(utilityPlatform);
        }

        public static void EnableFileSystem(IFileSystemPlatform fileSystemPlatform)
        {
            _filesystem = new FileSystem(fileSystemPlatform);
        }
    
        public static void EnableDataStorage(IDataStoragePlatform sqlitePlatform)
        {                        
            FileUri storageUri = _filesystem.CreateFileUri(AppController.Globals.DatabaseFilePath);
            _database = new DataStorage(sqlitePlatform, storageUri);
        }

        public static void EnableServices(IServiceClientPlatform servicePlatform)
        {
            _services = new ServiceClient(servicePlatform, AppController.Globals.ServicesBaseUrl);
            _services.RequestTimeout = AppController.Globals.ServicesDefaultRequestTimeout;
            _services.AccessTokenName = "x-zumo-auth";
        }

        #endregion

        #region Users Methods    

        public static async Task RegisterUser(CancellationTokenSource cts,
            string email,
            string password,
            Action<Poco.User> success,
            Action<string> error,
            Action finished)
        {
            try
            {
                var response = await _services.Request<Dto.Response<Poco.User>>(
                    // Resource to call
                    "users/register",
                    // HTTP method
                    Method.PUT,
                    // Cancellation token
                    cts.Token,
                    // Parameters handling
                    ParametersHandling.Body,
                    // Parameters
                    new
                    {
                        email = email,
                        password = password
                    });

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {                    
                    success?.Invoke(response.Data.Content);
                }
                else
                {
                    error?.Invoke(
                        response.Data.ExceptionMessage ?? response.Data.Message ?? response.StatusDescription);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                                
                error?.Invoke("Internal error :(");
            }
            finally
            {                
                finished?.Invoke();                
            }
        }

        public static async Task LoginUser(CancellationTokenSource cts,
            string email,
            string password,
            Action<Poco.User> success,
            Action<string> error,
            Action finished)
        {
            try
            {
                var response = await _services.Request<Dto.Response<Poco.User>>(
                    // Resource to call
                    "users/login",
                    // HTTP method
                    Method.POST,
                    // Cancellation token
                    cts.Token,
                    // Parameters handling
                    ParametersHandling.Body,
                    // Parameters
                    new
                    {
                        email = email,
                        password = password
                    });

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string accessToken = response.Data.Content.AuthAccessToken;
                    DateTime accessExpirationDate = response.Data.Content.AuthExpirationDate.GetValueOrDefault().ToLocalTime();

                    // Refresh access token for further service calls
                    _services.RefreshAccessToken(accessToken, accessExpirationDate);
                    
                    success?.Invoke(response.Data.Content);
                }
                else
                {
                    error?.Invoke(
                        response.Data.ExceptionMessage ?? response.Data.Message ?? response.StatusDescription);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                
                error?.Invoke("Internal error :(");
            }
            finally
            {                
                finished?.Invoke();
            }
        }

        public static async Task VerifyUser(CancellationTokenSource cts,
            string email,
            string password,
            Action success,
            Action<string> error,
            Action finished)
        {
            try
            {
                var response = await _services.Request<Dto.Response>(
                    // Resource to call
                    "users/verify",
                    // HTTP method
                    Method.POST,
                    // Cancellation token
                    cts.Token,
                    // Parameters handling
                    ParametersHandling.Body,
                    // Parameters
                    new
                    {
                        email = email,
                        password = password
                    });

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {                    
                    success?.Invoke();
                }
                else
                {
                    error?.Invoke(
                        response.Data.ExceptionMessage ?? response.Data.Message ?? response.StatusDescription);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                
                error?.Invoke("Internal error :(");
            }
            finally
            {                
                finished?.Invoke();
            }
        }

        public static async Task RestoreUser(CancellationTokenSource cts,
            string accessToken,
            Action<Poco.User> success,
            Action<string> error,
            Action finished)
        {
            try
            {
                var response = await _services.Request<Dto.Response<Poco.User>>(
                    // Resource to call
                    "users/restore",
                    // HTTP method
                    Method.GET,
                    // Cancellation token
                    cts.Token,
                    // Parameters handling
                    ParametersHandling.Default,
                    // Parameters
                    new
                    {
                        accessToken = accessToken
                    });

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {                    
                    DateTime accessExpirationDate = response.Data.Content.AuthExpirationDate.GetValueOrDefault().ToLocalTime();

                    // Refresh access token for further service calls
                    _services.RefreshAccessToken(accessToken, accessExpirationDate);
                    
                    success?.Invoke(response.Data.Content);
                }
                else
                {
                    error?.Invoke(
                        response.Data.ExceptionMessage ?? response.Data.Message ?? response.StatusDescription);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());

                error?.Invoke("Internal error :(");
            }
            finally
            {                
                finished?.Invoke();
            }
        }

        #endregion

        #region Tasks Methods

        public static async Task AddTodoItem(CancellationTokenSource cts,
            int userId,
            string title,
            string description,
            int willDoIn,
            string tags,
            Action<TodoItem> success,
            Action<string> error,
            Action finished)
        {
            try
            {
                var response = await _services.Request<Dto.Response<Poco.TodoItem>>(
                    // Resource to call
                    "todo/addnew",
                    // HTTP method
                    Method.PUT,
                    // Cancellation token
                    cts.Token,
                    // Parameters handling
                    ParametersHandling.Body,
                    // Parameters
                    new
                    {
                        UserId = userId,
                        Title = title,
                        Description = description,
                        WillDoIn = willDoIn,
                        Tags = tags
                    });

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var x = response.Data.Content;
                    TodoItem todoItem = new TodoItem
                    {
                        TodoItemId = x.TodoItemId,
                        UserId = x.UserId,
                        Title = x.Title,
                        Description = x.Description,
                        CreationDate = x.CreationDate?.ToLocalTime(),
                        WillDoIn = x.WillDoIn,
                        Tags = x.Tags,
                        IsComplete = x.IsComplete,
                        CompletionDate = x.CompletionDate?.ToLocalTime()
                    };

                    _database.Insert(todoItem);

                    success?.Invoke(todoItem);
                }
                else if(response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    error?.Invoke("You must login again!");
                }
                else
                {                    
                    error?.Invoke(
                        response.Data.ExceptionMessage ?? response.Data.Message ?? response.StatusDescription);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                
                error?.Invoke("Internal error :(");
            }
            finally
            {                
                finished?.Invoke();
            }
        }

        public static async Task UpdateTodoItem(CancellationTokenSource cts,
            int todoItemId,
            string title,
            string description,
            int willDoIn,
            string tags,
            Action<TodoItem> success,
            Action<string> error,
            Action finished)
        {
            try
            {
                var response = await _services.Request<Dto.Response<Poco.TodoItem>>(
                    // Resource to call
                    "todo/update",
                    // HTTP method
                    Method.POST,
                    // Cancellation token
                    cts.Token,
                    // Parameters handling
                    ParametersHandling.Body,
                    // Parameters
                    new
                    {
                        TodoItemId = todoItemId,
                        Title = title,
                        Description = description,
                        WillDoIn = willDoIn,
                        Tags = tags
                    });

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    TodoItem todoItem = null;
                    _database.RunInTransaction(t =>
                        {
                            var i = response.Data.Content;
                            todoItem = t.FindSingle<TodoItem>(x => x.TodoItemId == i.TodoItemId);
                            todoItem.Title = i.Title;
                            todoItem.Description = i.Description;
                            todoItem.WillDoIn = i.WillDoIn;
                            todoItem.Tags = tags;

                            t.Update(todoItem);
                        });
                    
                    success?.Invoke(todoItem);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    error?.Invoke("You must login again!");
                }
                else
                {
                    error?.Invoke(
                        response.Data.ExceptionMessage ?? response.Data.Message ?? response.StatusDescription);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());

                error?.Invoke("Internal error :(");
            }
            finally
            {
                finished?.Invoke();
            }
        }

        public static async Task CompleteTodoItem(CancellationTokenSource cts,
            int todoItemId,
            bool complete,
            Action<TodoItem> success,
            Action<string> error,
            Action finished)
        {
            try
            {
                var response = await _services.Request<Dto.Response<Poco.TodoItem>>(
                    // Resource to call
                    complete ? "todo/complete" : "todo/uncomplete",
                    // HTTP method
                    Method.POST,
                    // Cancellation token
                    cts.Token,
                    // Parameters handling
                    ParametersHandling.Body,
                    // Parameters
                    todoItemId);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {                    
                    TodoItem todoItem = null;
                    _database.RunInTransaction(t =>
                    {
                        var i = response.Data.Content;
                        todoItem = t.FindSingle<TodoItem>(x => x.TodoItemId == i.TodoItemId);
                        todoItem.IsComplete = i.IsComplete;
                        todoItem.CompletionDate = i.CompletionDate?.ToLocalTime();

                        t.Update(todoItem);
                    });


                    success?.Invoke(todoItem);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    error?.Invoke("You must login again!");
                }
                else
                {
                    error?.Invoke(
                        response.Data.ExceptionMessage ?? response.Data.Message ?? response.StatusDescription);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());

                error?.Invoke("Internal error :(");
            }
            finally
            {
                finished?.Invoke();
            }
        }

        public static async Task DeleteTodoItem(CancellationTokenSource cts,
            int todoItemId,
            Action success,
            Action<string> error,
            Action finished)
        {
            try
            {
                var response = await _services.Request<Dto.Response<Poco.TodoItem>>(
                    // Resource to call
                    "todo/delete",
                    // HTTP method
                    Method.DELETE,
                    // Cancellation token
                    cts.Token,
                    // Parameters handling
                    ParametersHandling.Default,
                    // Parameters
                    new
                    {
                        itemId = todoItemId
                    });

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    TodoItem todoItem = null;
                    _database.RunInTransaction(t =>
                    {                        
                        todoItem = t.FindSingle<TodoItem>(x => x.TodoItemId == todoItemId);
                        t.Delete(todoItem);
                    });

                    success?.Invoke();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    error?.Invoke("You must login again!");
                }
                else
                {
                    error?.Invoke(
                        response.Data.ExceptionMessage ?? response.Data.Message ?? response.StatusDescription);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());

                error?.Invoke("Internal error :(");
            }
            finally
            {
                finished?.Invoke();
            }
        }

        public static async Task RefreshTodoItems(CancellationTokenSource cts,
            int userId,            
            Action<TodoItem[]> success,
            Action<string> error,
            Action finished)
        {
            try
            {
                var response = await _services.Request<Dto.Response<Poco.WorkList>>(
                    // Resource to call
                    "todo/mylist",
                    // HTTP method
                    Method.GET,
                    // Cancellation token
                    cts.Token,
                    // Parameters Handling
                    ParametersHandling.Default,
                    // Parameters
                    new
                    {
                        userId = userId,                        
                    });

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    TodoItem[] todoItems = response.Data.Content.Items
                        .Select(x => new TodoItem
                        {
                            TodoItemId = x.TodoItemId,
                            UserId = x.UserId,
                            Title = x.Title,
                            Description = x.Description,
                            CreationDate = x.CreationDate?.ToLocalTime(),
                            WillDoIn = x.WillDoIn,
                            Tags = x.Tags,
                            IsComplete = x.IsComplete,
                            CompletionDate = x.CompletionDate?.ToLocalTime()
                        })
                        .ToArray();

                    _database.RunInTransaction(t =>
                        {
                            t.DeleteAll<TodoItem>(x => x.UserId == userId);

                            if (todoItems.Length > 0)
                                t.InsertAll(todoItems);
                        });
                    
                    success?.Invoke(todoItems);
                }
                else if(response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    error?.Invoke("You must login again!");
                }
                else
                {
                    error?.Invoke(
                        response.Data.ExceptionMessage ?? response.Data.Message ?? response.StatusDescription);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                
                error?.Invoke("Internal error :(");
            }
            finally
            {                
                finished?.Invoke();
            }
        }

        #endregion

        #region Helper Methods

        #endregion
    }
}
