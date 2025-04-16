using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIaaS.Migrations
{
    public partial class Added_NlpEntities : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NickName",
                table: "AbpUsers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NlpChatbots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    GreetingMsg = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    FailedMsg = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    AlternativeQuestion = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    Language = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Disabled = table.Column<bool>(type: "bit", nullable: false),
                    ChatbotPictureId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LineToken = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    FacebookAccessToken = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    FacebookVerifyToken = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    FacebookSecretKey = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    EnableWebChat = table.Column<bool>(type: "bit", nullable: false),
                    EnableHttpChat = table.Column<bool>(type: "bit", nullable: false),
                    EnableFacebook = table.Column<bool>(type: "bit", nullable: false),
                    EnableLine = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleterUserId = table.Column<long>(type: "bigint", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NlpChatbots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NlpClientInfos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConnectionProtocol = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    UpdatedTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IP = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ClientChannel = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NlpClientInfos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NlpFacebookUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    PictureUrl = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NlpFacebookUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NlpLineUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    PictureUrl = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NlpLineUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NlpTenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    NlpPriority = table.Column<double>(type: "float", nullable: false),
                    SubscriptionAmount = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NlpTenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NlpTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NlpTokenType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NlpTokenValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleterUserId = table.Column<long>(type: "bigint", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NlpTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NlpCbDictionaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    Word = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    Synonym = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    Vector = table.Column<string>(type: "nvarchar(max)", maxLength: 65536, nullable: true),
                    IsDisabled = table.Column<bool>(type: "bit", nullable: false),
                    Scope = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Language = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    HostDicId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NlpChatbotId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NlpCbDictionaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NlpCbDictionaries_NlpChatbots_NlpChatbotId",
                        column: x => x.NlpChatbotId,
                        principalTable: "NlpChatbots",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "NlpCbModels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    NlpCbMLanguage = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    NlpCbMStatus = table.Column<int>(type: "int", nullable: false),
                    NlpCbMTrainingStartTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NlpCbMTrainingCompleteTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NlpCbMTrainingCancellationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NlpCbMInfo = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    NlpCbMCreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    NlpCbMCreationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NlpCbAccuracy = table.Column<double>(type: "float", nullable: true),
                    NlpChatbotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NlpCbMTrainingCancellationUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NlpCbModels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NlpCbModels_AbpUsers_NlpCbMCreatorUserId",
                        column: x => x.NlpCbMCreatorUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NlpCbModels_AbpUsers_NlpCbMTrainingCancellationUserId",
                        column: x => x.NlpCbMTrainingCancellationUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NlpCbModels_NlpChatbots_NlpChatbotId",
                        column: x => x.NlpChatbotId,
                        principalTable: "NlpChatbots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NlpCbTrainingDatas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    NlpCbTDSource = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NlpNNIDRepetition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NlpChatbotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NlpCbTrainingDatas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NlpCbTrainingDatas_NlpChatbots_NlpChatbotId",
                        column: x => x.NlpChatbotId,
                        principalTable: "NlpChatbots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NlpQAs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Question = table.Column<string>(type: "nvarchar(max)", maxLength: 8192, nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(max)", maxLength: 8192, nullable: true),
                    QuestionCategory = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    SegmentStatus = table.Column<int>(type: "int", nullable: false),
                    SegmentErrorMsg = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    NNID = table.Column<int>(type: "int", nullable: false),
                    QaType = table.Column<int>(type: "int", nullable: true),
                    QuestionCount = table.Column<int>(type: "int", nullable: false),
                    NlpChatbotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NlpQAs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NlpQAs_NlpChatbots_NlpChatbotId",
                        column: x => x.NlpChatbotId,
                        principalTable: "NlpChatbots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NlpWorkflows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Disabled = table.Column<bool>(type: "bit", nullable: false),
                    NlpChatbotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleterUserId = table.Column<long>(type: "bigint", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NlpWorkflows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NlpWorkflows_NlpChatbots_NlpChatbotId",
                        column: x => x.NlpChatbotId,
                        principalTable: "NlpChatbots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NlpCbMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NlpMessage = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    NlpMessageType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    NlpSenderRole = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    NlpReceiverRole = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    NlpCreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClientReadTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AlternativeQuestion = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    AgentReadTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    QAAccuracyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Invalid = table.Column<bool>(type: "bit", nullable: false),
                    NlpChatbotId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NlpAgentId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NlpCbMessages", x => x.Id);
                    //table.ForeignKey(
                    //    name: "FK_NlpCbMessages_AbpUsers_NlpAgentId",
                    //    column: x => x.NlpAgentId,
                    //    principalTable: "AbpUsers",
                    //    principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NlpCbMessages_NlpChatbots_NlpChatbotId",
                        column: x => x.NlpChatbotId,
                        principalTable: "NlpChatbots",
                        principalColumn: "Id");
                    //table.ForeignKey(
                    //    name: "FK_NlpCbMessages_NlpFacebookUsers_ClientId",
                    //    column: x => x.ClientId,
                    //    principalTable: "NlpFacebookUsers",
                    //    principalColumn: "Id");
                    //table.ForeignKey(
                    //    name: "FK_NlpCbMessages_NlpLineUsers_ClientId",
                    //    column: x => x.ClientId,
                    //    principalTable: "NlpLineUsers",
                    //    principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "NlpCbTrainedAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    NlpCbTAAnswer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NNID = table.Column<int>(type: "int", nullable: false),
                    NlpCbTrainingDataId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NlpCbTrainedAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NlpCbTrainedAnswers_NlpCbTrainingDatas_NlpCbTrainingDataId",
                        column: x => x.NlpCbTrainingDataId,
                        principalTable: "NlpCbTrainingDatas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NlpCbQAAccuracies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Question = table.Column<string>(type: "nvarchar(max)", maxLength: 8192, nullable: false),
                    AnswerAcc1 = table.Column<double>(type: "float", nullable: true),
                    AnswerAcc2 = table.Column<double>(type: "float", nullable: true),
                    AnswerAcc3 = table.Column<double>(type: "float", nullable: true),
                    NlpChatbotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnswerId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AnswerId2 = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AnswerId3 = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NlpCbQAAccuracies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NlpCbQAAccuracies_NlpChatbots_NlpChatbotId",
                        column: x => x.NlpChatbotId,
                        principalTable: "NlpChatbots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NlpCbQAAccuracies_NlpQAs_AnswerId1",
                        column: x => x.AnswerId1,
                        principalTable: "NlpQAs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NlpCbQAAccuracies_NlpQAs_AnswerId2",
                        column: x => x.AnswerId2,
                        principalTable: "NlpQAs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NlpCbQAAccuracies_NlpQAs_AnswerId3",
                        column: x => x.AnswerId3,
                        principalTable: "NlpQAs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "NlpWorkflowStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    StateName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    StateInstruction = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    Vector = table.Column<string>(type: "nvarchar(max)", maxLength: 65536, nullable: true),
                    AcceptIncomingStatus = table.Column<string>(type: "nvarchar(max)", maxLength: 65536, nullable: true),
                    OutgoingFalseOp = table.Column<string>(type: "nvarchar(max)", maxLength: 65536, nullable: true),
                    Outgoing3FalseOp = table.Column<string>(type: "nvarchar(max)", maxLength: 65536, nullable: true),
                    ResponseNonWorkflowAnswer = table.Column<bool>(type: "bit", nullable: false),
                    NlpWorkflowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NlpWorkflowStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NlpWorkflowStates_NlpWorkflows_NlpWorkflowId",
                        column: x => x.NlpWorkflowId,
                        principalTable: "NlpWorkflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NlpCbDictionaries_NlpChatbotId",
                table: "NlpCbDictionaries",
                column: "NlpChatbotId");

            migrationBuilder.CreateIndex(
                name: "IX_NlpCbDictionaries_TenantId",
                table: "NlpCbDictionaries",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_NlpCbMessages_ClientId",
                table: "NlpCbMessages",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_NlpCbMessages_NlpAgentId",
                table: "NlpCbMessages",
                column: "NlpAgentId");

            migrationBuilder.CreateIndex(
                name: "IX_NlpCbMessages_NlpChatbotId",
                table: "NlpCbMessages",
                column: "NlpChatbotId");

            migrationBuilder.CreateIndex(
                name: "IX_NlpCbMessages_TenantId",
                table: "NlpCbMessages",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_NlpCbModels_NlpCbMCreatorUserId",
                table: "NlpCbModels",
                column: "NlpCbMCreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_NlpCbModels_NlpCbMTrainingCancellationUserId",
                table: "NlpCbModels",
                column: "NlpCbMTrainingCancellationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_NlpCbModels_NlpChatbotId",
                table: "NlpCbModels",
                column: "NlpChatbotId");

            migrationBuilder.CreateIndex(
                name: "IX_NlpCbModels_TenantId",
                table: "NlpCbModels",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_NlpCbQAAccuracies_AnswerId1",
                table: "NlpCbQAAccuracies",
                column: "AnswerId1");

            migrationBuilder.CreateIndex(
                name: "IX_NlpCbQAAccuracies_AnswerId2",
                table: "NlpCbQAAccuracies",
                column: "AnswerId2");

            migrationBuilder.CreateIndex(
                name: "IX_NlpCbQAAccuracies_AnswerId3",
                table: "NlpCbQAAccuracies",
                column: "AnswerId3");

            migrationBuilder.CreateIndex(
                name: "IX_NlpCbQAAccuracies_NlpChatbotId",
                table: "NlpCbQAAccuracies",
                column: "NlpChatbotId");

            migrationBuilder.CreateIndex(
                name: "IX_NlpCbQAAccuracies_TenantId",
                table: "NlpCbQAAccuracies",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_NlpCbTrainedAnswers_NlpCbTrainingDataId",
                table: "NlpCbTrainedAnswers",
                column: "NlpCbTrainingDataId");

            migrationBuilder.CreateIndex(
                name: "IX_NlpCbTrainedAnswers_TenantId",
                table: "NlpCbTrainedAnswers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_NlpCbTrainingDatas_NlpChatbotId",
                table: "NlpCbTrainingDatas",
                column: "NlpChatbotId");

            migrationBuilder.CreateIndex(
                name: "IX_NlpCbTrainingDatas_TenantId",
                table: "NlpCbTrainingDatas",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_NlpChatbots_TenantId",
                table: "NlpChatbots",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_NlpClientInfos_TenantId",
                table: "NlpClientInfos",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_NlpQAs_NlpChatbotId",
                table: "NlpQAs",
                column: "NlpChatbotId");

            migrationBuilder.CreateIndex(
                name: "IX_NlpQAs_TenantId",
                table: "NlpQAs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_NlpTenants_TenantId",
                table: "NlpTenants",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_NlpWorkflows_NlpChatbotId",
                table: "NlpWorkflows",
                column: "NlpChatbotId");

            migrationBuilder.CreateIndex(
                name: "IX_NlpWorkflows_TenantId",
                table: "NlpWorkflows",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_NlpWorkflowStates_NlpWorkflowId",
                table: "NlpWorkflowStates",
                column: "NlpWorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_NlpWorkflowStates_TenantId",
                table: "NlpWorkflowStates",
                column: "TenantId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NlpCbDictionaries");

            migrationBuilder.DropTable(
                name: "NlpCbMessages");

            migrationBuilder.DropTable(
                name: "NlpCbModels");

            migrationBuilder.DropTable(
                name: "NlpCbQAAccuracies");

            migrationBuilder.DropTable(
                name: "NlpCbTrainedAnswers");

            migrationBuilder.DropTable(
                name: "NlpClientInfos");

            migrationBuilder.DropTable(
                name: "NlpTenants");

            migrationBuilder.DropTable(
                name: "NlpTokens");

            migrationBuilder.DropTable(
                name: "NlpWorkflowStates");

            migrationBuilder.DropTable(
                name: "NlpFacebookUsers");

            migrationBuilder.DropTable(
                name: "NlpLineUsers");

            migrationBuilder.DropTable(
                name: "NlpQAs");

            migrationBuilder.DropTable(
                name: "NlpCbTrainingDatas");

            migrationBuilder.DropTable(
                name: "NlpWorkflows");

            migrationBuilder.DropTable(
                name: "NlpChatbots");

            migrationBuilder.DropColumn(
                name: "NickName",
                table: "AbpUsers");
        }
    }
}
