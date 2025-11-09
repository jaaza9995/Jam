using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jam.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Firstname = table.Column<string>(type: "TEXT", nullable: false),
                    Lastname = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "Stories",
                columns: table => new
                {
                    StoryId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    DifficultyLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    Accessible = table.Column<int>(type: "INTEGER", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: true),
                    Played = table.Column<int>(type: "INTEGER", nullable: false),
                    Finished = table.Column<int>(type: "INTEGER", nullable: false),
                    Failed = table.Column<int>(type: "INTEGER", nullable: false),
                    Dnf = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stories", x => x.StoryId);
                    table.ForeignKey(
                        name: "FK_Stories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "EndingScenes",
                columns: table => new
                {
                    EndingSceneId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EndingType = table.Column<int>(type: "INTEGER", nullable: false),
                    EndingText = table.Column<string>(type: "TEXT", nullable: false),
                    StoryId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EndingScenes", x => x.EndingSceneId);
                    table.ForeignKey(
                        name: "FK_EndingScenes_Stories_StoryId",
                        column: x => x.StoryId,
                        principalTable: "Stories",
                        principalColumn: "StoryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntroScenes",
                columns: table => new
                {
                    IntroSceneId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IntroText = table.Column<string>(type: "TEXT", nullable: false),
                    StoryId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntroScenes", x => x.IntroSceneId);
                    table.ForeignKey(
                        name: "FK_IntroScenes_Stories_StoryId",
                        column: x => x.StoryId,
                        principalTable: "Stories",
                        principalColumn: "StoryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayingSessions",
                columns: table => new
                {
                    PlayingSessionId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Score = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxScore = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentSceneId = table.Column<int>(type: "INTEGER", nullable: true),
                    CurrentSceneType = table.Column<int>(type: "INTEGER", nullable: true),
                    StoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayingSessions", x => x.PlayingSessionId);
                    table.ForeignKey(
                        name: "FK_PlayingSessions_Stories_StoryId",
                        column: x => x.StoryId,
                        principalTable: "Stories",
                        principalColumn: "StoryId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayingSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "QuestionScenes",
                columns: table => new
                {
                    QuestionSceneId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SceneText = table.Column<string>(type: "TEXT", nullable: false),
                    Question = table.Column<string>(type: "TEXT", nullable: false),
                    NextQuestionSceneId = table.Column<int>(type: "INTEGER", nullable: true),
                    StoryId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionScenes", x => x.QuestionSceneId);
                    table.ForeignKey(
                        name: "FK_QuestionScenes_QuestionScenes_NextQuestionSceneId",
                        column: x => x.NextQuestionSceneId,
                        principalTable: "QuestionScenes",
                        principalColumn: "QuestionSceneId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuestionScenes_Stories_StoryId",
                        column: x => x.StoryId,
                        principalTable: "Stories",
                        principalColumn: "StoryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnswerOptions",
                columns: table => new
                {
                    AnswerOptionId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Answer = table.Column<string>(type: "TEXT", nullable: false),
                    FeedbackText = table.Column<string>(type: "TEXT", nullable: false),
                    IsCorrect = table.Column<bool>(type: "INTEGER", nullable: false),
                    QuestionSceneId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnswerOptions", x => x.AnswerOptionId);
                    table.ForeignKey(
                        name: "FK_AnswerOptions_QuestionScenes_QuestionSceneId",
                        column: x => x.QuestionSceneId,
                        principalTable: "QuestionScenes",
                        principalColumn: "QuestionSceneId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnswerOptions_QuestionSceneId",
                table: "AnswerOptions",
                column: "QuestionSceneId");

            migrationBuilder.CreateIndex(
                name: "IX_EndingScenes_StoryId",
                table: "EndingScenes",
                column: "StoryId");

            migrationBuilder.CreateIndex(
                name: "IX_IntroScenes_StoryId",
                table: "IntroScenes",
                column: "StoryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayingSessions_StoryId",
                table: "PlayingSessions",
                column: "StoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayingSessions_UserId",
                table: "PlayingSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionScenes_NextQuestionSceneId",
                table: "QuestionScenes",
                column: "NextQuestionSceneId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuestionScenes_StoryId",
                table: "QuestionScenes",
                column: "StoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Stories_UserId",
                table: "Stories",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnswerOptions");

            migrationBuilder.DropTable(
                name: "EndingScenes");

            migrationBuilder.DropTable(
                name: "IntroScenes");

            migrationBuilder.DropTable(
                name: "PlayingSessions");

            migrationBuilder.DropTable(
                name: "QuestionScenes");

            migrationBuilder.DropTable(
                name: "Stories");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
