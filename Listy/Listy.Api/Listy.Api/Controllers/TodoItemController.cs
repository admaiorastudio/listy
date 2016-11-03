namespace AdMaiora.Listy.Api.Controllers
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Mail;
    using System.Web.Http;
    using System.Web.Http.Tracing;
    using System.Web.Security;
    using System.Collections.Generic;
    using System.IO;
    using System.Web;
    using System.Linq;
    using System.Data.Entity;
    using System.Threading.Tasks;

    using AdMaiora.Listy.Api.Models;
    using AdMaiora.Listy.Api.DataObjects;

    using Microsoft.Azure.Mobile.Server;
    using Microsoft.Azure.Mobile.Server.Config;
    using Microsoft.Azure.NotificationHubs;

    public class TodoItemController : ApiController
    {
        #region Constructors

        public TodoItemController()
        {
        }

        #endregion

        #region Users Endpoint Methods

        [Authorize]
        [HttpPost, Route("todo/addnew")]
        public IHttpActionResult AddNew(Poco.TodoItem item)
        {
            if(item.UserId <= 0)
                return BadRequest("User ID is not valid!");

            if (String.IsNullOrWhiteSpace(item.Title))
                return BadRequest("Title is not valid!");

            try
            {
                using (var ctx = new ListyDbContext())
                {
                    TodoItem ti = new TodoItem
                    {
                        UserId = item.UserId,
                        Title = item.Title,
                        Description = item.Description,
                        CreationDate = DateTime.Now.ToUniversalTime(),
                        Tags = item.Tags
                    };

                    ctx.TodoItems.Add(ti);

                    ctx.SaveChanges();

                    return Ok(Dto.Wrap(new Poco.TodoItem
                    {
                        TodoItemId = ti.TodoItemId,
                        UserId = ti.UserId,
                        Title = ti.Title,
                        Description = ti.Description,
                        CreationDate = ti.CreationDate,
                        Tags = ti.Tags
                    }));
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Authorize]
        [HttpPost, Route("todo/update")]
        public IHttpActionResult Update(Poco.TodoItem item)
        {
            if (item.TodoItemId <= 0)
                return BadRequest("TodoItem ID is not valid!");

            if (String.IsNullOrWhiteSpace(item.Title))
                return BadRequest("Title is not valid!");

            try
            {
                using (var ctx = new ListyDbContext())
                {
                    TodoItem ti = ctx.TodoItems.SingleOrDefault(x => x.TodoItemId == item.TodoItemId);
                    if (item == null)
                        return InternalServerError(new InvalidOperationException("Invalid TodoItem ID or User ID!"));

                    ti.Title = item.Title;
                    ti.Description = item.Description;
                    ti.Tags = item.Tags;

                    ctx.SaveChanges();

                    return Ok(Dto.Wrap(new Poco.TodoItem
                    {
                        TodoItemId = ti.TodoItemId,
                        UserId = ti.UserId,
                        Title = ti.Title,
                        Description = ti.Description,
                        CreationDate = ti.CreationDate,
                        Tags = ti.Tags,
                        IsComplete = ti.IsComplete,
                        CompletionDate = ti.CompletionDate                        
                    }));
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Authorize]
        [HttpPost, Route("todo/complete")]
        public IHttpActionResult Complete(int itemId)
        {
            if (itemId <= 0)
                return BadRequest("TodoItem ID is not valid!");

            try
            {
                using (var ctx = new ListyDbContext())
                {
                    TodoItem ti = ctx.TodoItems.SingleOrDefault(x => x.TodoItemId == itemId);
                    if (ti == null)
                        return InternalServerError(new InvalidOperationException("Invalid TodoItem ID!"));

                    ti.IsComplete = true;
                    ti.CompletionDate = DateTime.Now.ToUniversalTime();

                    ctx.SaveChanges();

                    return Ok(Dto.Wrap(new Poco.TodoItem
                    {
                        TodoItemId = ti.TodoItemId,
                        UserId = ti.UserId,
                        Title = ti.Title,
                        Description = ti.Description,
                        CreationDate = ti.CreationDate,
                        Tags = ti.Tags,
                        IsComplete = ti.IsComplete,
                        CompletionDate = ti.CompletionDate
                    }));
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Authorize]
        [HttpPost, Route("todo/delete")]
        public IHttpActionResult Delete(int itemId)
        {
            if (itemId <= 0)
                return BadRequest("TodoItem ID is not valid!");

            try
            {
                using (var ctx = new ListyDbContext())
                {
                    TodoItem ti = ctx.TodoItems.SingleOrDefault(x => x.TodoItemId == itemId);
                    if (ti == null)
                        return InternalServerError(new InvalidOperationException("Invalid TodoItem ID!"));

                    ctx.TodoItems.Remove(ti);

                    ctx.SaveChanges();

                    return Ok();
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Authorize]
        [HttpGet, Route("todo/mylist")]
        public IHttpActionResult GetMyList(int userId)
        {
            if (userId <= 0)
                return BadRequest("User ID is not valid!");

            try
            {
                using (var ctx = new ListyDbContext())
                {
                    User user = ctx.Users.SingleOrDefault(x => x.UserId == userId);
                    if (user == null)
                        return InternalServerError(new InvalidOperationException("The User ID you provide is invalid!"));

                    return Ok(Dto.Wrap(new Poco.WorkList
                    {
                        Items = ctx.TodoItems
                            .Where(x => x.UserId == userId)
                            .Select(x => new Poco.TodoItem
                            {
                                TodoItemId = x.TodoItemId,
                                UserId = x.UserId,
                                Title = x.Title,
                                Description = x.Description,
                                CreationDate = x.CreationDate,
                                Tags = x.Tags,
                                IsComplete = x.IsComplete,
                                CompletionDate = x.CompletionDate
                            })
                            .ToArray()
                    }));
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }

        }

        #endregion
    }
}
