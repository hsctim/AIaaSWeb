# AIaaS Web and ChatbotBert Projects

This README provides an overview of the AIaaS Web (frontend) and ChatbotBert (backend) projects, their features, architecture, and instructions on creating and testing a chatbot using the platform. Both projects are designed to work together as part of a multi-tenant chatbot platform.

---

# AIaaS Web (Frontend Web of Chatbot Platform) [https://chatbot1.azurewebsites.net/](https://chatbot1.azurewebsites.net/)

## Overview
AIaaS Web is a multi-tenant, web-based Chatbot SaaS platform designed for Chatbot services. It provides a user-friendly interface for managing and deploying chatbots, making it suitable for various applications.

## Key Features
- **NLP-Powered**: Utilizes BERT for advanced sentence and context understanding, enhancing chatbot interactions.
- **Multi-Tenant**: Effectively manages resources across multiple tenants for scalability.
- **User-Friendly**: Offers an intuitive interface for managing chatbots and QAs with ease.
- **Chatbot Management**: Simplifies the creation, training, and testing of chatbots.
- **QA Management**: Enables seamless addition, editing, and deletion of QAs for each chatbot.
- **Model Management**: Facilitates efficient training and management of chatbot models.
- **Web-Based**: Provides responsive design for accessibility on both mobile and desktop devices.
- **Fast Response**: Powered by Signal-R for real-time interactions with fallback transport methods, supporting protocols like WebSocket, long polling, and server-sent events.


## Architecture (Frontend)
- **Modular Design**: Implements Domain-Driven Design (DDD) for enhanced maintainability and scalability.
- **Multi-Tenancy**: Ensures efficient resource management across multiple tenants.
- **Comprehensive Testing**: Includes a robust testing framework for quality assurance.
- **CI/CD Pipeline**: Supports continuous integration and deployment for streamlined development workflows.
- **Localization Support**: Provides multi-language support and localization capabilities.
- **Customizable Interface**: Offers customizable themes (dark/light mode) and branding options for a tailored user experience.
- **Advanced Security**: Features role-based access control, Anti-DDoS, Anti-CSRF, and Multi-Factor Authentication (MFA).
- **Flexible Authentication**: Supports OAuth, JWT, and other authentication methods.
- **Optimized Performance**: Utilizes caching mechanisms for faster performance.
- **Real-Time Updates**: Enables real-time notifications and updates for dynamic interactions.
- **Responsive Design**: Ensures compatibility with both mobile and desktop devices.
- **Data Isolation**: Provides multi-tenant security with robust data isolation mechanisms.

## Comparison with ChatGPT
AIaaS was developed before ChatGPT using legacy technologies like BERT and traditional NLP machine learning. While functional, it lacks the advanced performance and capabilities of ChatGPT, which leverages cutting-edge AI advancements. Development on AIaaS was discontinued after ChatGPT's release.

---

## How to Create and Test Your Chatbot

1. **Access the Web Application**  
   Open the web application in your browser: [https://chatbot1.azurewebsites.net/](https://chatbot1.azurewebsites.net/)

2. **Sign Up or Log In**  
   - Choose a free plan for validation or testing purposes.  
   - After signing up, verify your email using the link sent to your inbox.  
   - Log in to the web application.

3. **Dashboard**  
   Once logged in, you will be directed to the dashboard where you can manage your chatbots.

4. **Create a Chatbot**  
   - Navigate to the "Chatbots" (問答機器人) section on the left sidebar.  
   - Click the "Create" button to create a new chatbot.

5. **Add QAs**  
   - Go to the "QAs" (問答設定) section on the left sidebar.  
   - Click the "Create" button to add new QAs. You can create multiple QAs for each chatbot.

6. **Train the Chatbot**  
   - Return to the "Chatbots" (問答機器人) section.  
   - Click the "Training" (訓練狀態) button to train the chatbot.  
   - Wait until the training status changes to "Completed" (訓練完成).

7. **Test the Chatbot**  
   - In the "Chatbots" (問答機器人) section, click the "Operations" (操作) button.  
   - Select "Webpage" (開啟交談視窗) to open the chatbot webpage.  
   - Enter "hello" in the input box and click "Send" to see the chatbot's response.

---

# ChatbotBert (Backend Service of Chatbot Platform)

## Overview
ChatbotBert is a Python-based NLP chatbot API powered by BERT, designed for seamless integration and building intelligent conversational agents.

## Key Features
- **NLP-Powered**: Uses BERT for enhanced understanding of word context.
- **Training**: Model training and fine-tuning capabilities.
- **Integration**: RESTful API endpoints for easy integration.
- **Efficiency**: Lightweight design optimized for 1-core CPU and 2GB memory.
- **Extensibility**: Easily extendable for additional features.

## Comparison with ChatGPT
ChatbotBert, developed prior to ChatGPT, focuses on lightweight and efficient NLP tasks for multi-tenant SaaS applications. Built on BERT, it excels in understanding word context within sentences. Unlike ChatGPT, a general-purpose conversational AI model, ChatbotBert is tailored for specific NLP tasks, prioritizing efficiency, resource management, and scalability with minimal computational resources.

---

## Contact
For questions or support, please contact [tim.chu@hotmail.com](mailto:tim.chu@hotmail.com).