using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Web.Migrations
{
    public partial class AddRateToCustomer : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            
            migrationBuilder.AddColumn<int>("Rate","Customer",nullable:false,defaultValue:0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            
        }
    }
}
