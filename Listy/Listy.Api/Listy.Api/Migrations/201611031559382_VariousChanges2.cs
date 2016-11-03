namespace AdMaiora.Listy.Api.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class VariousChanges2 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.TodoItems", "WillDoIn", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.TodoItems", "WillDoIn");
        }
    }
}
