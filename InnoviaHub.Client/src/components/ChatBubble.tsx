import { useEffect, useRef, useState, type FormEvent } from "react";
import { BASE_API_URL } from "../config/api";

type ChatMessage = {
  id: number;
  role: "user" | "assistant";
  content: string;
};

type ChatApiResponse = {
  answer: string;
};

const CHAT_URL = `${BASE_API_URL}/api/Chat`;

export default function ChatBubble() {
  const [isOpen, setIsOpen] = useState(false);
  const [question, setQuestion] = useState("");
  const [isSending, setIsSending] = useState(false);
  const [error, setError] = useState("");
  const [messages, setMessages] = useState<ChatMessage[]>([
    {
      id: 1,
      role: "assistant",
      content: "Hej! Hur kan jag hjälpa dig idag?",
    },
  ]);
  const messagesEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages, isOpen]);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const trimmedQuestion = question.trim();

    if (!trimmedQuestion || isSending) return;

    const userMessage: ChatMessage = {
      id: Date.now(),
      role: "user",
      content: trimmedQuestion,
    };

    setMessages((currentMessages) => [...currentMessages, userMessage]);
    setQuestion("");
    setError("");
    setIsSending(true);

    try {
      const response = await fetch(CHAT_URL, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify({ question: trimmedQuestion }),
      });

      if (!response.ok) {
        throw new Error("Förlåt, nu förstår jag inte riktigt.");
      }

      const data = await response.json() as ChatApiResponse;
      setMessages((currentMessages) => [
        ...currentMessages,
        {
          id: Date.now() + 1,
          role: "assistant",
          content: data.answer,
        },
      ]);
    } catch (sendError) {
      setError(sendError instanceof Error ? sendError.message : "Något gick fel.");
    } finally {
      setIsSending(false);
    }
  }

  return (
    <div className="fixed bottom-6 right-5 z-[100] sm:bottom-8 sm:right-8">
      {isOpen && (
        <section
          aria-label="Chatt med InnoviaHub"
          className="mb-4 flex h-[min(32rem,calc(100vh-8rem))] w-[calc(100vw-2.5rem)] max-w-sm flex-col overflow-hidden rounded-2xl"
          style={{
            background: "#0d1824",
            border: "1px solid #1e3347",
            boxShadow: "0 20px 60px rgba(0,0,0,0.45)",
          }}
        >
          <header className="flex items-center justify-between px-4 py-3" style={{ borderBottom: "1px solid #1e3347" }}>
            <div>
              <p className="font-semibold" style={{ color: "#e2eaf2", fontFamily: "Outfit, sans-serif" }}>InnoviaHub support</p>
              <p className="text-xs" style={{ color: "#7a94aa" }}>Ställ en fråga till assistenten</p>
            </div>
            <button
              type="button"
              onClick={() => setIsOpen(false)}
              aria-label="Stäng chatten"
              className="flex h-8 w-8 items-center justify-center rounded-lg text-lg transition-colors hover:bg-white/10"
              style={{ color: "#7a94aa" }}
            >
              x
            </button>
          </header>

          <div className="flex-1 space-y-3 overflow-y-auto p-4">
            {messages.map((message) => (
              <div key={message.id} className={`flex ${message.role === "user" ? "justify-end" : "justify-start"}`}>
                <p
                  className="max-w-[85%] whitespace-pre-wrap rounded-2xl px-3 py-2 text-sm"
                  style={{
                    background: message.role === "user" ? "#00d4aa" : "#111e2d",
                    color: message.role === "user" ? "#080e14" : "#e2eaf2",
                    border: message.role === "assistant" ? "1px solid #1e3347" : "none",
                  }}
                >
                  {message.content}
                </p>
              </div>
            ))}
            {isSending && <p className="text-xs" style={{ color: "#7a94aa" }}>Assistenten skriver...</p>}
            {error && <p className="text-xs" style={{ color: "#f43f5e" }}>{error}</p>}
            <div ref={messagesEndRef} />
          </div>

          <form onSubmit={handleSubmit} className="flex gap-2 p-3" style={{ borderTop: "1px solid #1e3347" }}>
            <input
              value={question}
              onChange={(event) => setQuestion(event.target.value)}
              placeholder="Skriv din fråga..."
              aria-label="Skriv din fråga"
              disabled={isSending}
              className="min-w-0 flex-1 rounded-xl px-3 py-2 text-sm outline-none"
              style={{ background: "#111e2d", border: "1px solid #1e3347", color: "#e2eaf2" }}
            />
            <button
              type="submit"
              disabled={isSending || !question.trim()}
              aria-label="Skicka fråga"
              className="rounded-xl px-3 py-2 text-sm font-semibold transition-opacity disabled:cursor-not-allowed disabled:opacity-40"
              style={{ background: "#00d4aa", color: "#080e14" }}
            >
              Skicka
            </button>
          </form>
        </section>
      )}

      <button
        type="button"
        onClick={() => setIsOpen((open) => !open)}
        aria-label={isOpen ? "Stäng chatt" : "Öppna chatt"}
        className="flex h-14 w-14 items-center justify-center rounded-full text-2xl font-semibold transition-transform hover:scale-105"
        style={{ background: "#00d4aa", color: "#080e14", boxShadow: "0 8px 28px rgba(0,212,170,0.3)" }}
      >
        {isOpen ? "x" : "?"}
      </button>
    </div>
  );
}
