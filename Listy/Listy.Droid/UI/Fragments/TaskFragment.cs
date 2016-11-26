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

    using AdMaiora.AppKit.UI;

    using AdMaiora.Listy.Model;

    #pragma warning disable CS4014
    public class TaskFragment : AdMaiora.AppKit.UI.App.Fragment
    {
        #region Inner Classes
        #endregion

        #region Constants and Fields

        private int _userId;

        private int _willDoIn;

        private TodoItem _item;

        // This flag check if we are already calling the login REST service
        private bool _isSendingTodoItem;
        // This cancellation token is used to cancel the rest send message request
        private CancellationTokenSource _cts0;

        #endregion

        #region Widgets

        [Widget]
        private RelativeLayout TitleLayout;

        [Widget]
        private ImageView TitleImage;

        [Widget]
        private EditText TitleText;

        [Widget]
        private RelativeLayout DueDateLayout;

        [Widget]
        private ImageView DueDateImage;

        [Widget]
        private TextView WillDoLabel;

        [Widget]
        private TextView DueDateLabel;

        [Widget]
        private Button DaysButton;

        [Widget]
        private RelativeLayout TagsLayout;

        [Widget]
        private ImageView TagsImage;

        [Widget]
        private EditText TagsText;

        [Widget]
        private RelativeLayout DescriptionLayout;

        [Widget]
        private View SideBar;

        [Widget]
        private EditText DescriptionText;

        [Widget]
        private TextView DescriptionLabel;

        [Widget]
        private RelativeLayout DetailsLayout;

        [Widget]
        private TextView CreatedLabel;

        [Widget]
        private TextView CreatedDateLabel;

        [Widget]
        private TextView CompletedLabel;

        [Widget]
        private TextView CompletedDateLabel;

        [Widget]
        private View BlockLayout;

        #endregion

        #region Constructors

        public TaskFragment()
        {
        }

        #endregion

        #region Properties
        #endregion

        #region Fragment Methods

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _willDoIn = 5;

            _userId = this.Arguments.GetInt("UserId");
            _item = this.Arguments.GetObject<TodoItem>("Item");
        }

        public override void OnCreateView(LayoutInflater inflater, ViewGroup container)
        {
            base.OnCreateView(inflater, container);

            #region Desinger Stuff
            
            SetContentView(Resource.Layout.FragmentTask, inflater, container);
            
            SlideUpToShowKeyboard();

            this.HasOptionsMenu = true;

            #endregion           

            this.Title = "New Item";

            this.DescriptionLabel.Clickable = true;
            this.DescriptionLabel.SetOnTouchListener(GestureListener.ForSingleTapUp(this.Activity,
                (e) =>
                {
                    this.DismissKeyboard();

                    var f = new TextInputFragment();
                    f.ContentText = this.DescriptionText.Text;
                    f.TextInputDone += TextInputFragment_TextInputDone;
                    this.FragmentManager.BeginTransaction()
                        .AddToBackStack("BeforeTextInputFragment")
                        .Replace(Resource.Id.ContentLayout, f, "TextInputFragment")
                        .Commit();
                }));


            this.DaysButton.Click += DaysButton_Click;

            if (_item != null)
                LoadItem(_item);

            this.BlockLayout.Visibility = _item == null || !_item.IsComplete ? ViewStates.Gone : ViewStates.Visible;
        }

        public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater)
        {
            base.OnCreateOptionsMenu(menu, inflater);

            menu.Clear();
            menu.Add(0, 1, 0, "Save").SetShowAsAction(ShowAsAction.Always);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch(item.ItemId)
            {
                case 1:

                    if (_item == null)
                        AddTodoItem();
                    else
                        UpdateTodoItem();

                    return true;

                default:
                    return base.OnOptionsItemSelected(item);
            }            
        }

        public override void OnDestroyView()
        {
            base.OnDestroyView();

            if (_cts0 != null)
                _cts0.Cancel();

            this.DaysButton.Click -= DaysButton_Click;
        }

        #endregion

        #region Public Methods
        #endregion

        #region Methods

        private void LoadItem(TodoItem item)
        {
            this.TitleText.Text = item.Title;
            this.TagsText.Text = item.Tags;
            this.CreatedDateLabel.Text = item.CreationDate?.ToString("G") ?? "n/a";
            this.CompletedDateLabel.Text = item.CompletionDate?.ToString("G") ?? "n/a";

            SetWillDoInDays(item.WillDoIn);
            SetDescription(item.Description);
        }

        private void SetDescription(string description)
        {
            this.DescriptionLabel.Text = !String.IsNullOrWhiteSpace(description) ? String.Empty : "Write here some notes...";
            this.DescriptionText.Text = description;
        }

        private void SetWillDoInDays(int willDoIn)
        {
            _willDoIn = willDoIn;
            if (_willDoIn < 0)
                _willDoIn = 5;

            Color color = ViewBuilder.ColorFromARGB(AppController.Colors.Green);
            if (_willDoIn < 2)
            {
                color = ViewBuilder.ColorFromARGB(AppController.Colors.Red);
            }
            else if (_willDoIn < 4)
            {
                color = ViewBuilder.ColorFromARGB(AppController.Colors.Orange);
            }

            this.DaysButton.Text = _willDoIn.ToString();
            this.DaysButton.SetTextColor(color);
        }

        private bool ValidateInput()
        {
            var validator = new WidgetValidator()
                .AddValidator(() => this.TitleText.Text, WidgetValidator.IsNotNullOrEmpty, "Please insert a title!")
                .AddValidator(() => this.DescriptionText.Text, WidgetValidator.IsNotNullOrEmpty, "Please insert a description!")
                .AddValidator(() => this.TagsText.Text, (string s) => String.IsNullOrWhiteSpace(s) || !s.Contains(" "), "Tags must be comma separated list, no blanks!");

            string errorMessage;
            if (!validator.Validate(out errorMessage))
            {
                Toast.MakeText(this.Activity.ApplicationContext, errorMessage, ToastLength.Long).Show();

                return false;
            }

            return true;
        }

        private void AddTodoItem()
        {
            if (_isSendingTodoItem)
                return;

            if (!ValidateInput())
                return;

            string title = this.TitleText.Text;
            string description = this.DescriptionText.Text;
            string tags = this.TagsText.Text;

            _isSendingTodoItem = true;
            ((MainActivity)this.Activity).BlockUI();

            _cts0 = new CancellationTokenSource();
            AppController.AddTodoItem(_cts0,
                _userId,
                title,
                description,
                _willDoIn,
                tags,
                (todoItem) =>
                {
                    this.FragmentManager.PopBackStack();
                },
                (error) =>
                {
                    Toast.MakeText(this.Activity.ApplicationContext, error, ToastLength.Long).Show();
                },
                () =>
                {
                    _isSendingTodoItem = false;
                    ((MainActivity)this.Activity).UnblockUI();
                });
        }

        private void UpdateTodoItem()
        {
            if (_isSendingTodoItem)
                return;

            if (!ValidateInput())
                return;

            string title = this.TitleText.Text;
            string description = this.DescriptionText.Text;
            string tags = this.TagsText.Text;

            _isSendingTodoItem = true;
            ((MainActivity)this.Activity).BlockUI();

            _cts0 = new CancellationTokenSource();
            AppController.UpdateTodoItem(_cts0,
                _item.TodoItemId,
                title,
                description,
                _willDoIn,
                tags,
                (todoItem) =>
                {
                    this.FragmentManager.PopBackStack();
                },
                (error) =>
                {
                    Toast.MakeText(this.Activity.ApplicationContext, error, ToastLength.Long).Show();
                },
                () =>
                {
                    _isSendingTodoItem = false;
                    ((MainActivity)this.Activity).UnblockUI();
                });
        }

        #endregion

        #region Event Handlers

        private void TextInputFragment_TextInputDone(object sender, TextInputDoneEventArgs e)
        {
            SetDescription(e.Text);
        }

        private void DaysButton_Click(object sender, EventArgs e)
        {
            SetWillDoInDays(_willDoIn - 1);
        }

        #endregion
    }
}