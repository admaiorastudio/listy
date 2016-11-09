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
    using AppKit.UI.App;

    #pragma warning disable CS4014
    public partial class AgendaViewController : AdMaiora.AppKit.UI.App.UISubViewController
    {
        #region Inner Classes

        private class AgendaViewSource : UIItemListViewSource<TodoItem>
        {
            #region Constants and Fields
            
            private List<string> _palette;
            private Dictionary<string, string> _colors;

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

                cell.TitleLabel.Text = item.Title + " " + item.WillDoIn.ToString();
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

            this.TaskList.RowHeight = UITableView.AutomaticDimension;
            this.TaskList.EstimatedRowHeight = 46;            
            this.TaskList.BackgroundColor = ViewBuilder.ColorFromARGB(AppController.Colors.White);
            this.TaskList.TableFooterView = new UIView(CoreGraphics.CGRect.Empty);
            this.TaskList.ItemCommand += TaskList_ItemCommand;

            RefreshItems();
        }

        public override bool CreateBarButtonItems(UIBarButtonCreator items)
        {
            items
                .AddItem("Add New", UIBarButtonItemStyle.Plain);

            return true;
        }

        public override bool BarButtonItemSelected(int index)
        {
            switch(index)
            {
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

            this.TaskList.ItemCommand -= TaskList_ItemCommand;
        }

        #endregion

        #region Public Methods
        #endregion

        #region Methods

        private void RefreshItems()
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
                    if (_source == null)
                    {
                        _source = new AgendaViewSource(this, items);
                        this.TaskList.Source = _source;
                    }
                    else
                    {
                        _source.Refresh(items);
                        this.TaskList.ReloadData();              
                    }
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

        #endregion

        #region Event Handlers

        private void TaskList_ItemCommand(object sender, UIItemListCommandEventArgs e)
        {
            TodoItem item = e.UserData as TodoItem;
            switch(e.Command)
            {
                case "SetAsDone":
                    item.IsComplete = true;
                    break;

                case "SetAsNotDone":
                    item.IsComplete = false;
                    break;
            }

            this.TaskList.ReloadData();
        }

        #endregion
    }
}


