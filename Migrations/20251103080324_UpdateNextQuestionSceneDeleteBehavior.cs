using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jam.Migrations
{
    /// <inheritdoc />
    public partial class UpdateNextQuestionSceneDeleteBehavior : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuestionScenes_QuestionScenes_NextQuestionSceneId",
                table: "QuestionScenes");

            migrationBuilder.AddForeignKey(
                name: "FK_QuestionScenes_QuestionScenes_NextQuestionSceneId",
                table: "QuestionScenes",
                column: "NextQuestionSceneId",
                principalTable: "QuestionScenes",
                principalColumn: "QuestionSceneId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuestionScenes_QuestionScenes_NextQuestionSceneId",
                table: "QuestionScenes");

            migrationBuilder.AddForeignKey(
                name: "FK_QuestionScenes_QuestionScenes_NextQuestionSceneId",
                table: "QuestionScenes",
                column: "NextQuestionSceneId",
                principalTable: "QuestionScenes",
                principalColumn: "QuestionSceneId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
