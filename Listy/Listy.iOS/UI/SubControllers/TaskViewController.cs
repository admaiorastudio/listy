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
    public partial class TaskViewController : AdMaiora.AppKit.UI.App.UISubViewController
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

        #region Constructors

        public TaskViewController()
            : base("TaskViewController", null)
        {
        }

        #endregion

        #region Properties
        #endregion

        #region ViewController Methods

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            _willDoIn = 5;

            _userId = this.Arguments.GetInt("UserId");
            _item = this.Arguments.GetObject<TodoItem>("Item");
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            #region Designer Stuff
            
            var texts = new[] { this.TitleText, this.TagsText };
            this.AutoShouldReturnTextFields(texts);
            this.AutoDismissTextFields(texts);

            this.NavigationItem.BackBarButtonItem = new UIBarButtonItem("Back", UIBarButtonItemStyle.Plain, null);

            this.HasBarButtonItems = true;
            
            SlideUpToShowKeyboard();

            #endregion

            this.Title = "New Item";

            this.DescriptionLabel.UserInteractionEnabled = true;
            this.DescriptionLabel.AddGestureRecognizer(new UITapGestureRecognizer(
                () =>
                {
                    this.DismissKeyboard();

                    var c = new TextInputViewController();
                    c.ContentText = this.DescriptionText.Text;
                    c.TextInputDone += TextInputViewController_TextInputDone;
                    this.NavigationController.PushViewController(c, false);

                }));

            this.DaysButton.TouchUpInside += DaysButton_TouchUpInside;
            
            if (_item != null)
                LoadItem(_item);

            this.BlockLayout.Hidden = _item == null || !_item.IsComplete;
        }

        public override bool CreateBarButtonItems(UIBarButtonCreator items)
        {
            items.AddItem("Save", UIBarButtonItemStyle.Plain);
            return true;
        }

        public override bool BarButtonItemSelected(int index)
        {
            switch (index)
            {
                case 0:

                    if (_item == null)
                        AddTodoItem();
                    else
                        UpdateTodoItem();

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

            this.DaysButton.TouchUpInside -= DaysButton_TouchUpInside;
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
           
            UIColor color = ViewBuilder.ColorFromARGB(AppController.Colors.Green);
            if (_willDoIn < 2)
            {
                color = ViewBuilder.ColorFromARGB(AppController.Colors.Red);
            }
            else if (_willDoIn < 4)
            {
                color = ViewBuilder.ColorFromARGB(AppController.Colors.Orange);
            }

            this.DaysButton.SetTitle(_willDoIn.ToString(), UIControlState.Normal);
            this.DaysButton.SetTitleColor(color, UIControlState.Normal);
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
                UIToast.MakeText(errorMessage, UIToastLength.Long).Show();

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
            ((MainViewController)this.MainViewController).BlockUI();
            
            _cts0 = new CancellationTokenSource();
            AppController.AddTodoItem(_cts0,
                _userId,
                title,
                description,
                _willDoIn,
                tags,
                (todoItem) =>
                {
                    this.NavigationController.PopViewController(true);
                },
                (error) =>
                {
                    UIToast.MakeText(error, UIToastLength.Long).Show();
                },
                () =>
                {
                    _isSendingTodoItem = false;
                    ((MainViewController)this.MainViewController).UnblockUI();
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
            ((MainViewController)this.MainViewController).BlockUI();

            _cts0 = new CancellationTokenSource();
            AppController.UpdateTodoItem(_cts0,
                _item.TodoItemId,
                title,
                description,
                _willDoIn,
                tags,
                (todoItem) =>
                {
                    this.NavigationController.PopViewController(true);
                },
                (error) =>
                {
                    UIToast.MakeText(error, UIToastLength.Long).Show();
                },
                () =>
                {
                    _isSendingTodoItem = false;
                    ((MainViewController)this.MainViewController).UnblockUI();
                });
        }

        #endregion

        #region Event Handlers

        private void TextInputViewController_TextInputDone(object sender, TextInputDoneEventArgs e)
        {
            SetDescription(e.Text);
        }

        private void DaysButton_TouchUpInside(object sender, EventArgs e)
        {
            SetWillDoInDays(_willDoIn - 1);
        }

        #endregion
    }
}


