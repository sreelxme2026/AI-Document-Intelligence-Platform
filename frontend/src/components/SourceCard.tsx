import type { RetrievalSource } from "../types/api";

interface Props {
  source: RetrievalSource;
  index: number;
}

const SourceCard = ({ source, index }: Props) => {
  return (
    <details className="source-card">
      <summary>
        Source {index + 1}
        {source.pageNumber
          ? ` • Page ${source.pageNumber}`
          : ""}
      </summary>

      <p>{source.content}</p>

      <small>
        Similarity: {(source.similarityScore * 100).toFixed(1)}%
      </small>
    </details>
  );
};

export default SourceCard;