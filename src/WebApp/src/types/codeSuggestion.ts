export type PRCodeSuggestion = {
  suggestion_content: string;
  existing_code: string;
  improved_code: string;
  one_sentence_summary: string;
  relevant_lines_start: number;
  relevant_lines_end: number;
  label: string;
};

export type PRCodeImprovement = {
  relevant_file: string;
  language: string;
  code_suggestions: PRCodeSuggestion[];
};
