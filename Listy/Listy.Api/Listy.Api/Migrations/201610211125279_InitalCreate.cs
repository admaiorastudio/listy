 namespace AdMaiora.Listy.Api.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitalCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.TodoItems",
                c => new
                    {
                        TodoItemId = c.Int(nullable: false, identity: true),
                        Title = c.String(),
                        Description = c.String(),
                        CreationDate = c.DateTime(nullable: false),
                        Tags = c.String(),
                    })
                .PrimaryKey(t => t.TodoItemId);
            
            CreateTable(
                "dbo.Users",
                c => new
                    {
                        UserId = c.Int(nullable: false, identity: true),
                        Email = c.String(),
                        Password = c.String(),
                        LoginDate = c.DateTime(),
                        LastActiveDate = c.DateTime(),
                        AuthAccessToken = c.String(),
                        AuthExpirationDate = c.DateTime(),
                        Ticket = c.String(),
                        IsConfirmed = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.UserId);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Users");
            DropTable("dbo.TodoItems");
        }
    }
}
