namespace AdMaiora.Listy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;

    using Foundation;
    using UIKit;

    using AdMaiora.AppKit.UI;

    using AdMaiora.Listy.Model;    

    #pragma warning disable CS4014
    public partial class AgendaViewController : AdMaiora.AppKit.UI.App.UISubViewController
    {
        #region Inner Classes

        private class AgendaViewSource : UIItemListViewSource<TodoItem>
        {
            #region Constants and Fields           
            #endregion

            #region Constructors

            public AgendaViewSource(UIViewController controller, IEnumerable<TodoItem> source)
                : base(controller, "TaskViewCell", source)
            {
            }

            #endregion

            #region Public Methods

            public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath, UITableViewCell cellView, TodoItem item)
            {
                var cell = cellView as TaskViewCell;

                string checkImage = "image_check_selected";
                UIColor taskColor = ViewBuilder.ColorFromARGB(AppController.Colors.AshGray);
                
                if (!item.IsComplete)
                {
                    checkImage = "image_check_empty_green";

                    DateTime dueDate = item.CreationDate.GetValueOrDefault().Date.AddDays(item.WillDoIn);
                    int dueDays = (dueDate - DateTime.Now.Date).Days;
                    taskColor = ViewBuilder.ColorFromARGB(AppController.Colors.Green);
                    if (dueDays < 2)
                    {
                        checkImage = "image_check_empty_red";
                        taskColor = ViewBuilder.ColorFromARGB(AppController.Colors.Red);
                    }
                    else if (dueDays < 4)
                    {
                        checkImage = "image_check_empty_orange";
                        taskColor = ViewBuilder.ColorFromARGB(AppController.Colors.Orange);
                    }
                }

                cell.CheckButton.SetImage(UIImage.FromBundle(checkImage), UIControlState.Normal);

                cell.SelectionStyle = UITableViewCellSelectionStyle.None;

                cell.TitleLabel.Text = item.Title;
                cell.TitleLabel.TextColor = taskColor;

                return cell;
            }

            public void Clear()
            {
                this.SourceItems.Clear();
            }

            public void Refresh(IEnumerable<TodoItem> items)
            {
                this.SourceItems.Clear();
                this.SourceItems.AddRange(items);
            }

            public void SetAsCompleted(int itemId, bool isComplete, DateTime? completionDate = null)
            {
                TodoItem item = this.SourceItems.SingleOrDefault(x => x.TodoItemId == itemId);
                if (item == null)
                    return;

                item.IsComplete = isComplete;
                item.CompletionDate = completionDate;
            }

            #endregion

            #region Methods

            protected override void GetViewCreated(UITableView tableView, UITableViewCell cellView)
            {
                var cell = cellView as TaskViewCell;
                cell.CheckButton.TouchUpInside += (s, e) =>
                    {
                        var item = GetItemFromView(cell);
                        ExecuteCommand(tableView, item.IsComplete ? "SetAsNotDone" : "SetAsDone", item);                        
                    };
            }
            
            #endregion
        }

        #endregion

        #region Constants and Fields

        private int _userId;

        private AgendaViewSource _source;

        // This flag check if we are already calling the login REST service
        private bool _isRefreshingItems;
        // This cancellation token is used to cancel the rest send message request
        private CancellationTokenSource _cts0;


        #endregion

        #region Constructors

        public AgendaViewController()
            : base("AgendaViewController", null)
        {
        }

        #endregion

        #region Properties
        #endregion

        #region ViewController Methods

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            _userId = this.Arguments.GetInt("UserId");
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            #region Designer Stuff

            this.HasBarButtonItems = true;

            #endregion

            this.Title = "Agenda";

            this.NavigationController.SetNavigationBarHidden(false, true);

            _source = new AgendaViewSource(this, new TodoItem[0]):
            this.TaskList.Source = _source;
            this.TaskList.RowHeight = UITableView.AutomaticDimension;
            this.TaskList.EstimatedRowHeight = 46;            
            this.TaskList.BackgroundColor = ViewBuilder.ColorFromARGB(AppController.Colors.White);
            this.TaskList.TableFooterView = new UIView(CoreGraphics.CGRect.Empty);
            this.TaskList.ItemSelected += TaskList_ItemSelected;
            this.TaskList.ItemLongPress += TaskList_ItemLongPress;
            this.TaskList.ItemCommand += TaskList_ItemCommand;            

            RefreshTodoItems();
        }

        public override bool CreateBarButtonItems(UIBarButtonCreator items)
        {
            items.AddItem("Add New", UIBarButtonItemStyle.Plain);
            return true;
        }

        public override bool BarButtonItemSelected(int index)
        {
            switch(index)
            {
                case AgendaViewController.BarButtonBack:

                    QuitAgenda();
                    return true;

                case 0:

                    var c = new TaskViewController();
                    c.Arguments = new UIBundle();
                    c.Arguments.PutInt("UserId", _userId);
                    this.NavigationController.PushViewController(c, true);

                    return true;

                default:
                    return base.BarButtonItemSelected(index);
            }            
        }

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);

            if (_cts0 != null)
                _cts0.Cancel();

            this.TaskList.ItemSelected -= TaskList_ItemSelected;
            this.TaskList.ItemLongPress -= TaskList_ItemLongPress;
            this.TaskList.ItemCommand -= TaskList_ItemCommand;
        }

        #endregion

        #region Public Methods
        #endregion

        #region Methods

        private void RefreshTodoItems()
        {
            if (_isRefreshingItems)
                return;

            this.TaskList.Hidden = true;

            _isRefreshingItems = true;
            ((MainViewController)this.MainViewController).BlockUI();
                                    
            _cts0 = new CancellationTokenSource();
            AppController.RefreshTodoItems(_cts0,
                _userId,
                (items) =>
                {
                    items = items
                        .OrderBy(x => (x.CreationDate.GetValueOrDefault().Date.AddDays(x.WillDoIn).Date - DateTime.Now.Date).Days)
                        .ToArray();

                    _source.Refresh(items);
                    this.TaskList.ReloadData();
                },
                (error) =>
                {
                    UIToast.MakeText(error, UIToastLength.Long).Show();
                },
                () =>
                {
                    this.TaskList.Hidden = false;

                    _isRefreshingItems = false;
                    ((MainViewController)this.MainViewController).UnblockUI();
                });
        }

        private void CompleteTodoItem(TodoItem item, bool completed)
        {
            if (_isRefreshingItems)
                return;

            _isRefreshingItems = true;
            ((MainViewController)this.MainViewController).BlockUI();

            _cts0 = new CancellationTokenSource();
            AppController.CompleteTodoItem(_cts0,
                item.TodoItemId,
                completed,
                (todoItem) =>
                {
                    _source.SetAsCompleted(todoItem.TodoItemId, todoItem.IsComplete, todoItem.CompletionDate);
                    this.TaskList.ReloadData();
                },
                (error) =>
                {
                    UIToast.MakeText(error, UIToastLength.Long).Show();
                },
                () =>
                {
                    _isRefreshingItems = false;
                    ((MainViewController)this.MainViewController).UnblockUI();
                });
        }

        private void DeleteTodoItem(TodoItem item)
        {
            if (_isRefreshingItems)
                return;
            
            _isRefreshingItems = true;
            ((MainViewController)this.MainViewController).BlockUI();

            _cts0 = new CancellationTokenSource();
            AppController.DeleteTodoItem(_cts0,
                item.TodoItemId,
                () =>
                {
                    _source.RemoveItem(item);              
                    this.TaskList.ReloadData();

                    UIToast.MakeText("An item has been removed!", UIToastLength.Long).Show();
                },
                (error) =>
                {
                    UIToast.MakeText(error, UIToastLength.Long).Show();
                },
                () =>
                {
                    _isRefreshingItems = false;
                    ((MainViewController)this.MainViewController).UnblockUI();
                });
        }

        private void QuitAgenda()
        {
            (new UIAlertViewBuilder(new UIAlertView()))
                .SetTitle("Leave the agenda?")
                .SetMessage("Press ok to leave the agenda now!")
                .AddButton("Ok",
                    (s, ea) =>
                    {
                        AppController.Settings.AuthAccessToken = null;
                        AppController.Settings.AuthExpirationDate = null;

                        this.DismissKeyboard();
                        this.NavigationController.PopViewController(true);
                    })
                .AddButton("Take me back",
                    (s, ea) =>
                    {
                    })
                .Show();
        }

        #endregion

        #region Event Handlers

        private void TaskList_ItemSelected(object sender, UIItemListSelectEventArgs e)
        {
            TodoItem item = e.Item as TodoItem;

            var c = new TaskViewController();
            c.Arguments = new UIBundle();
            c.Arguments.PutObject<TodoItem>("Item", item);
            this.NavigationController.PushViewController(c, true);
        }

        private void TaskList_ItemLongPress(object sender, UIItemListLongPressEventArgs e)
        {
            (new UIAlertViewBuilder(new UIAlertView()))
                .SetTitle("Delete this item from the list?")
                .SetMessage("Press ok to delete the item")
                .AddButton("Delete",
                    (s, ea) =>
                    {          
                        TodoItem item = e.Item as TodoItem;
                        DeleteTodoItem(item);                          
                    })
                .AddButton("No!",
                    (s, ea) =>
                    {
                        // Do Nothing!
                    })
                .Show();
        }

        private void TaskList_ItemCommand(object sender, UIItemListCommandEventArgs e)
        {
            TodoItem item = e.UserData as TodoItem;
            switch(e.Command)
            {
                case "SetAsDone":
                    CompleteTodoItem(item, true);
                    break;

                case "SetAsNotDone":
                    CompleteTodoItem(item, false);
                    break;
            }

            this.TaskList.ReloadData();
        }

        #endregion
    }
}


