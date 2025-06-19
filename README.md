# RAG Example Project

## 目標

1. 熟練 OpenAI API 的使用。
2. 練習兩個主題：
   - 文章翻譯：將 Reflections on Net Assessment - Interviews with Andrew W. Marshall 目錄下的英文 markdown 文章翻譯為中文（C# Console 程式）。
   - RAG Web 程式：以上述內容為知識庫，建立一個 Retrieval-Augmented Generation (RAG) Web 應用（C# Web 專案）。

## 執行步驟

### 1. 文章翻譯（C# Console 程式）
- 讀取指定目錄下的 markdown 檔案。
- 呼叫 OpenAI API 將英文翻譯為中文。
- 將翻譯結果存到另一個目錄。

### 2. RAG Web 程式（C# Web 專案）
- 以 markdown 內容作為知識庫。
- 實作簡單的檢索與生成 Web 介面。
- 使用 OpenAI API 回答用戶問題，並引用相關內容。

---

本專案將依照上述步驟逐步實作與記錄。
