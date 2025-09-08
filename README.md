# 🧠 AgenticUtilities.MessageReducer

A lightweight, testable C# library for reducing chat history size in agentic AI workflows using summarization. Designed for use with [Microsoft Semantic Kernel](https://github.com/microsoft/semantic-kernel), this utility helps maintain context within token limits by collapsing older conversation turns into concise summaries.

---

## ✨ Features

- Summarizes oldest N turns when token count exceeds model limits
- Uses Semantic Kernel's `IChatCompletionService` for LLM summarization
- Token-aware via `Tokenizer` (e.g., TiktokenTokenizer)
- Configurable thresholds via `MessageReducerOptions`
- Fully unit-testable with injectable dependencies
- Clean logging and cancellation support

---

## 📦 Installation

Clone the repo and add the project to your solution:

```bash
git clone https://github.com/spikesoftware/AgenticUtilities.git
