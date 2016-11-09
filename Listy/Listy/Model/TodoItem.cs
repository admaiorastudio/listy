namespace AdMaiora.Listy.Model
{
    using System;    
    using SQLite;

    public class TodoItem
    {
        [PrimaryKey]
        public int TodoItemId { get; set; }

        [Indexed]
        public int UserId { get; set; }

        [Indexed]
        public string Title { get; set; }

        public string Description { get; set; }

        [Indexed]
        public DateTime? CreationDate { get; set; }
        
        public int WillDoIn { get; set; }
        
        public string Tags { get; set; }

        public bool IsComplete { get; set; }

        [Indexed]
        public DateTime? CompletionDate { get; set; }
    }
}

