
# WebAPI

您可以透過WebAPI向ChatPal&#46;Ai傳送問題，並接受答案，以下是ChatPal&#46;Ai所提供的WebAPI, 

|功能|方法|URL|說明|
|---|---|---|---|
|**[SendMessage](#sendmessage)**|POST|/Chatbot/SendMessage|傳送一個問題至ChatPal&#46;Ai，並接收ChatPal.Ai回傳的答案。|
|**[GetMessage](#getmessage)**|POST|/Chatbot/GetMessage|接收由ChatPal&#46;Ai傳送的訊息，如果由客服代替機器人回應問題，透過GetMessage可接收客服主動傳送來的訊息。|
|**[SetWorkFlowState](#setworkflowstate)**|POST|/Chatbot/SetWorkFlowState|設定目前的工作流程狀態。|
|**[GetWorkFlowState](#getworkflowstate)**|POST|/Chatbot/GetWorkFlowState|取得目前的工作流程狀態。|
|**[ProfilePicture](#profilepicture)**|GET|/Chatbot/ProfilePicture/[id]|機器人的頭像圖檔|
|**[SessionInfo](#sessioninfo)**|GET|/Chatbot/SessionInfo/[id]|取得Session會話相關資訊|




## SendMessage

- **方法:** 
	POST
	
- **URL:** 
	/Chatbot/SendMessage
	
- **說明:** 
	傳送一個問題至ChatPal&#46;Ai，並接收ChatPal.Ai回傳的答案。
	
- **傳入值說明:** 
	
	|參數|類別|必選|說明|
	|---|---|---|---|
	|chatbotId|guid|√|問答機器人的GUID，表示訊息(問題)由哪一個問答機器人接收。|
	|clientId|guid|√|Client端的GUID，表示訊息(問題)由哪一個ClientId傳送。<br/><br/>如果您有多個終端設備要連結至同一個問答機器人，請設定為不同的GUID，以便辨別不同的終端設備。|
	|clientChannel|string|√|Client端的通道名稱。<br/><br/>可以設定通道名稱為Web, Device, Line或Facebook。在ChatPal.Ai的**值機操作**及**訊息記錄**中會顯示Client端的通道名稱，可以協助您在值機或分析時判斷訊息的來源通道。|
	|message|string|√|由Client端傳送至問答機器人的問題(訊息)。|


	**傳入的JSON範例:** 
	>{ "message" : "現在天氣" , "ClientChannel" : "POS" , "clientId" : "4ab13edf-2c5e-4fcf-b62a-b8f5629895ed" , "chatbotId" : "bb3bcb55-0b24-4679-87fe-373c01c1e521" }

<br/>


- **傳回值說明:** 

	|參數|類別|說明|
	|---|---|---|	
	|errorMessage|string|如果發生非預期的錯誤時，會列出錯誤資訊。|	
	|messages[]|[Messages類別](#messages) |問答機器人會回覆一個訊息(答案)、0個訊息或多個訊息至Client端。<br/><br/>如果問答機器人無法預測正確的答案且設定預測失敗的回覆內容為空白時，就不會回傳任何答案(0個訊息)。<br/><br/>問答機器人也可能會回傳多個訊息(包含AI運算出的預測答案及客服所傳送的訊息)，原因是訊仍存在Message Queue尚未被Client端取出，可以使用**GetMessages**取得在Queue中的訊息並清空Message Queue。<br/><br/>Messages的類別結構請參考下表[Messages類別](#messages)|	

	### Messages類別 ###
	|參數|類別|說明|
	|---|---|---|
	|chatbotId|guid|問答機器人的GUID，表示訊息(答案)由哪一個問答機器人傳送。|
	|clientId|guid|Client端的GUID，表示訊息(答案)由哪一個ClientId接收。|
	|clientChannel|string|Client端的通道名稱。在ChatPal.Ai的**值機操作**及**訊息記錄**中會顯示Client端的通道名稱，可以協助您在值機或分析時判斷訊息的來源通道。|
	|connectionProtocol|string|訊息傳送所使用的通訊協定，包含了`http`，`signalr`，`websocket`或其它協定。|				
	|message|string|由問答機器人傳送至Client端的答案(訊息)。|	
	|messageType|string|`text`表示回傳的是文字訊息。<br/>`text.error`表示回傳的是預測錯誤的文字訊息。<br/>`text.workflow`表示回傳的是文字訊息，且是位於工作流程時回傳的訊息。<br/>`text.workflow.text`表示回傳的是預測錯誤的文字訊息，且是位於工作流程時回傳的錯誤訊息。|
	|senderRole|string|預設值為`chatbot`|
	|senderName|string|問答機器人的名字|
	|senderImage|string|問答機器人的頭像圖檔url|
	|senderTime|string|問答機器人傳送訊息(答案)的時間(UTC)，|
	|receiverName|string|client端使用者的名字|	
	|receiverImage|string|client端使用者的頭像圖檔url，預設值為`/Common/Images/default-profile-picture.png`|	
	|receiverName|string|client端使用者的名字|			
	|receiverRole|string|預設值為`client`|			
	|messageDetails[]|[ChatbotMessageDetails類別](#chatbotmessagedetails)|回傳多個預測的答案內容，並依照準確度排序。<br/><br/>ChatbotMessageDetails的類別結構請參考下表[ChatbotMessageDetails類別](#chatbotmessagedetails)|				
	|workflow|string|目前的工作流程。|
	|workflowState|string|目前的工作流程的狀態。|
	|failedCount|int|問答機器人預測錯誤(低於特定的準確度)的連續次數。|

	{"messages":[{"id":"e536600d-6334-4baa-306a-08da0599e9ed","chatbotId":"2ff3d928-25ab-4b02-1d24-08d9e62952ee","clientId":"ad2f1df3-6cec-45f7-ba27-0dd9c08105f7","message":"*請輸入您的門號","messageType":"text.workflow","senderRole":"chatbot","senderName":"愛瑪","senderImage":"/Chatbot/ProfilePicture/61023cb4-fb49-b536-d42c-3a01cbd218bb","senderTime":"2022-03-14T09:06:25.2135402Z","receiverName":"","receiverImage":"/Common/Images/default-profile-picture.png","receiverRole":"client","clientChannel":"PostMan","messageDetails":[{"acc":0.9755151867866516,"messages":["*請輸入您的門號"]}],"connectionProtocol":"http","workflow":"自助服務牆.查繳帳單","workflowState":"查繳帳單.輸入門號","failedCount":0}]}

	### ChatbotMessageDetails類別 ###
	|參數|類別|說明|
	|---|---|---|
	|acc|float32|預測答案的準確度，最大值為1，最小值為0|
	|messages|string[]|問答機器人回應的答案。|
