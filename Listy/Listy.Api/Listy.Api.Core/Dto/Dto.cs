namespace AdMaiora.Listy.Api
{
    using System;
    
    public static class Poco
    {
        public class User
        {
            public int UserId;            
            public string Email;
            public string Password;
            public DateTime? LoginDate;
            public string AuthAccessToken;
            public DateTime? AuthExpirationDate;
        }

        public class TodoItem
        {
            public int TodoItemId;
            public int UserId;
            public string Title;
            public string Description;
            public DateTime? CreationDate;
            public int WillDoIn;
            public string Tags;
            public bool IsComplete;
            public DateTime? CompletionDate;
        }

        public class WorkList
        {
            public TodoItem[] Items;            
        }
    }

    public static class Dto
    {
        public class Response
        {
            public string Message;

            public string ExceptionMessage;
            public string ExceptionType;
        }

        public class Response<TContent> : Response
        {
            public TContent Content;

            public Response(TContent content)
            {
                this.Content = content;
            }
        }

        public static Response<T> Wrap<T>(T content)
        {
            return new Response<T>(content);
        }
    }
}