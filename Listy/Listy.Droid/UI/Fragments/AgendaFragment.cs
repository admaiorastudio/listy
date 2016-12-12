namespace AdMaiora.Listy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;

    using Android.App;
    using Android.Content;
    using Android.OS;
    using Android.Runtime;
    using Android.Util;
    using Android.Views;
    using Android.Widget;
    using Android.Graphics;
	using Android.Support.V7.Widget;

    using AdMaiora.AppKit.UI;

    using AdMaiora.Listy.Model;    

	#pragma warning disable CS4014
    public class AgendaFragment : AdMaiora.AppKit.UI.App.Fragment
    {
        #region Inner Classes
		
        class AgendaAdapter : ItemRecyclerAdapter<AgendaAdapter.ChatViewHolder, TodoItem>
        {
            #region Inner Classes

            public class ChatViewHolder : ItemViewHolder
            {
                [Widget]
                public ImageButton CheckButton;

                [Widget]
                public TextView TitleLabel;

                public ChatViewHolder(View itemView)
                    : base(itemView)
                {                    
                }
            }

            #endregion

            #region Costants and Fields

            private Random _rnd;

            private List<string> _palette;
            private Dictionary<string, string> _colors;

            #endregion

            #region Constructors

            public AgendaAdapter(AdMaiora.AppKit.UI.App.Fragment context, IEnumerable<TodoItem> source)
                : base(context, Resource.Layout.CellTask, source)
            {
            }

            #endregion

            #region Public Methods

            public override void GetView(int postion, ChatViewHolder holder, View view, TodoItem item)
            {
                string checkImage = "image_check_selected";
                Color taskColor = ViewBuilder.ColorFromARGB(AppController.Colors.AshGray);

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

                holder.CheckButton.SetImageResource(checkImage);

                holder.TitleLabel.Text = item.Title;
                holder.TitleLabel.SetTextColor(taskColor);
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

            protected override void GetViewCreated(RecyclerView.ViewHolder holder, View view, ViewGroup parent)
            {
                var cell = holder as ChatViewHolder;
                cell.CheckButton.Click += (s, e) =>
                    {
                        var item = GetItemFromView(view);
                        ExecuteCommand(parent, item.IsComplete ? "SetAsNotDone" : "SetAsDone", item);
                    };
            }

            #endregion
        }

        #endregion

        #region Constants and Fields

        private int _userId;

        private AgendaAdapter _adapter;

        // This flag check if we are already calling the login REST service
        private bool _isRefreshingItems;
        // This cancellation token is used to cancel the rest send message request
        private CancellationTokenSource _cts0;
        
        #endregion

        #region Widgets

        [Widget]
        private ItemRecyclerView TaskList;

        #endregion

        #region Constructors

        public AgendaFragment()
        {
        }

        #endregion

        #region Properties
        #endregion

        #region Fragment Methods

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _userId = this.Arguments.GetInt("UserId");
        }

        public override void OnCreateView(LayoutInflater inflater, ViewGroup container)
        {
            base.OnCreateView(inflater, container);

            #region Desinger Stuff

            SetContentView(Resource.Layout.FragmentAgenda, inflater, container);

            this.HasOptionsMenu = true;

            #endregion

            this.Title = "Agenda";

            this.ActionBar.Show();

            _adapter = new AgendaAdapter(this, new TodoItem[0]);
            this.TaskList.SetAdapter(_adapter);
            this.TaskList.ItemSelected += TaskList_ItemSelected;
            this.TaskList.ItemLongPress += TaskList_ItemLongPress;
            this.TaskList.ItemCommand += TaskList_ItemCommand;

            RefreshTodoItems();
        }

        public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater)
        {
            base.OnCreateOptionsMenu(menu, inflater);

            menu.Clear();
            menu.Add(0, 1, 0, "Add New").SetShowAsAction(ShowAsAction.Always);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch(item.ItemId)
            {
                case Android.Resource.Id.Home:
                    QuitAgenda();
                    return true;

                case 1:

                    var f = new TaskFragment();
                    f.Arguments = new Bundle();
                    f.Arguments.PutInt("UserId", _userId);
                    this.FragmentManager.BeginTransaction()
                        .AddToBackStack("BeforeTaskFragment")
                        .Replace(Resource.Id.ContentLayout, f, "TaskFragment")
                        .Commit();

                    return true;

                default:
                    return base.OnOptionsItemSelected(item); ;
            }            
        }

        public override bool OnBackButton()
        {
            QuitAgenda();
            return true;            
        }

        public override void OnDestroyView()
        {
            base.OnDestroyView();

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

            this.TaskList.Visibility = ViewStates.Gone;

            _isRefreshingItems = true;
            ((MainActivity)this.Activity).BlockUI();

            _cts0 = new CancellationTokenSource();
            AppController.RefreshTodoItems(_cts0,
                _userId,
                (items) =>
                {
                    items = items
                        .OrderBy(x => (x.CreationDate.GetValueOrDefault().Date.AddDays(x.WillDoIn).Date - DateTime.Now.Date).Days)
                        .ToArray();

                    _adapter.Refresh(items);
                    this.TaskList.ReloadData();
                },
                (error) =>
                {
                    Toast.MakeText(this.Activity.ApplicationContext, error, ToastLength.Long).Show();
                },
                () =>
                {
                    this.TaskList.Visibility = ViewStates.Visible;

                    _isRefreshingItems = false;
                    ((MainActivity)this.Activity).UnblockUI();
                });
        }

        private void CompleteTodoItem(TodoItem item, bool completed)
        {
            if (_isRefreshingItems)
                return;

            _isRefreshingItems = true;
            ((MainActivity)this.Activity).BlockUI();

            _cts0 = new CancellationTokenSource();
            AppController.CompleteTodoItem(_cts0,
                item.TodoItemId,
                completed,
                (todoItem) =>
                {
                    _adapter.SetAsCompleted(todoItem.TodoItemId, todoItem.IsComplete, todoItem.CompletionDate);
                    this.TaskList.ReloadData();
                },
                (error) =>
                {
                    Toast.MakeText(this.Activity.ApplicationContext, error, ToastLength.Long).Show();
                },
                () =>
                {
                    _isRefreshingItems = false;
                    ((MainActivity)this.Activity).UnblockUI();
                });
        }

        private void DeleteTodoItem(TodoItem item)
        {
            if (_isRefreshingItems)
                return;

            _isRefreshingItems = true;
            ((MainActivity)this.Activity).BlockUI();

            _cts0 = new CancellationTokenSource();
            AppController.DeleteTodoItem(_cts0,
                item.TodoItemId,
                () =>
                {
                    _adapter.RemoveItem(item);
                    this.TaskList.ReloadData();

                    Toast.MakeText(this.Activity.ApplicationContext, "An item has been removed!", ToastLength.Long).Show();
                },
                (error) =>
                {
                    Toast.MakeText(this.Activity.ApplicationContext, error, ToastLength.Long).Show();
                },
                () =>
                {
                    _isRefreshingItems = false;
                    ((MainActivity)this.Activity).UnblockUI();
                });
        }

        private void QuitAgenda()
        {
            (new AlertDialog.Builder(this.Activity))
                .SetTitle("Leave the agenda?")
                .SetMessage("Press ok to leave the agenda now!")
                .SetPositiveButton("Ok",
                    (s, ea) =>
                    {
                        AppController.Settings.AuthAccessToken = null;
                        AppController.Settings.AuthExpirationDate = null;

                        this.DismissKeyboard();
                        this.FragmentManager.PopBackStack();
                    })
                .SetNegativeButton("Take me back",
                    (s, ea) =>
                    {
                    })
                .Show();
        }

        #endregion

        #region Event Handlers
        private void TaskList_ItemSelected(object sender, ItemListSelectEventArgs e)
        {
            TodoItem item = e.Item as TodoItem;

            var f = new TaskFragment();
            f.Arguments = new Bundle();
            f.Arguments.PutObject("Item", item);
            this.FragmentManager.BeginTransaction()
                .AddToBackStack("BeforeTaskFragment")
                .Replace(Resource.Id.ContentLayout, f, "TaskFragment")
                .Commit();
        }

        private void TaskList_ItemLongPress(object sender, ItemListLongPressEventArgs e)
        {
            (new AlertDialog.Builder(this.Activity))
                .SetTitle("Delete this item from the list?")
                .SetMessage("Press ok to delete the item")
                .SetPositiveButton("Delete",
                    (s, ea) =>
                    {
                        TodoItem item = e.Item as TodoItem;
                        DeleteTodoItem(item);
                    })
                .SetNegativeButton("No!",
                    (s, ea) =>
                    {
                        // Do Nothing!
                    })
                .Show();
        }

        private void TaskList_ItemCommand(object sender, ItemListCommandEventArgs e)
        {
            TodoItem item = e.UserData as TodoItem;
            switch (e.Command)
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