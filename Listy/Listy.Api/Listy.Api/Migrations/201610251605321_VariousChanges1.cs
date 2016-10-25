namespace AdMaiora.Listy.Api.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class VariousChanges1 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.TodoItems", "UserId", c => c.Int(nullable: false));
            AddColumn("dbo.TodoItems", "IsComplete", c => c.Boolean(nullable: false));
            AddColumn("dbo.TodoItems", "CompletionDate", c => c.DateTime());
            CreateIndex("dbo.TodoItems", "UserId");
        }
        
        public override void Down()
        {
            DropIndex("dbo.TodoItems", new[] { "UserId" });
            DropColumn("dbo.TodoItems", "CompletionDate");
            DropColumn("dbo.TodoItems", "IsComplete");
            DropColumn("dbo.TodoItems", "UserId");
        }
    }
}
