import type { CompiledMemory } from '../types';

export function useOllama() {
  const OLLAMA_URL = 'http://localhost:11434/v1/chat/completions';

  const chat = async (
    prompt: string,
    memory: CompiledMemory,
    model: string = 'llama3.2',
    temperature: number = 0.2
  ): Promise<string> => {
    const systemPreamble = `You are a coding assistant. You MUST obey the Instruction Memory (IM) provided as JSON.
Cite IM rule ids in square brackets when you rely on them (e.g., [im:http.client@v3]).
If a user asks "why", summarize the relevant rules and their ids.`;

    const memoryJson = JSON.stringify(memory, null, 2);
    const systemMemory = `INSTRUCTIONS_MEMORY_JSON\n${memoryJson}`;

    const messages = [
      { role: 'system', content: systemPreamble },
      { role: 'system', content: systemMemory },
      { role: 'user', content: prompt },
    ];

    try {
      const response = await fetch(OLLAMA_URL, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          model,
          messages,
          temperature,
        }),
      });

      if (!response.ok) {
        throw new Error(`Ollama HTTP ${response.status}: ${await response.text()}`);
      }

      const data = await response.json();

      // Handle OpenAI-compatible response format
      if (data.choices && data.choices[0]?.message?.content) {
        return data.choices[0].message.content;
      }

      throw new Error('Unexpected response format from Ollama');
    } catch (error) {
      if (error instanceof Error) {
        // Check if Ollama is not running
        if (error.message.includes('fetch')) {
          throw new Error('Cannot connect to Ollama. Make sure Ollama is running at http://localhost:11434');
        }
        throw error;
      }
      throw new Error('Unknown error calling Ollama');
    }
  };

  return { chat };
}
