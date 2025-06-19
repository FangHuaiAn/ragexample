# RAGWeb

這是一個 ASP.NET Core Web API 專案，提供：
- RAG（Retrieval-Augmented Generation）問答：根據 /translated/Reflections 目錄下的分段翻譯 Markdown 內容，檢索相關段落並結合生成式 AI 回答。
- 文章自動摘要：針對指定文章產生摘要。

## 主要功能
- 讀取 /translated/Reflections 目錄下的 Markdown 檔案。
- 串接 OpenAI API 進行問答與摘要。
- 提供 RESTful API 端點。

## 啟動方式
```bash
dotnet run
```

## 目錄結構
- `/translated/Reflections`：分段與翻譯後的 Markdown 檔案
- `Controllers/`：API 控制器
- `Services/`：RAG 與摘要服務

## TODO
- 實作 RAG 問答 API
- 實作文章摘要 API
- 整合 OpenAI API
