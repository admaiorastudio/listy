namespace AdMaiora.Listy.Api.DataObjects
{    
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class TodoItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TodoItemId
        {
            get;
            set;
        }
        
        [Index]
        public int UserId
        {
            get;
            set;
        }

        public string Title
        {
            get;
            set;
        }

        public string Description
        {
            get;
            set;
        }

        public DateTime CreationDate
        {
            get;
            set;
        }

        public string Tags
        {
            get;
            set;
        }

        public int WillDoIn
        {
            get;
            set;
        }

        public bool IsComplete
        {
            get;
            set;
        }

        public DateTime? CompletionDate
        {
            get;
            set;
        } 
    }
}