type TextBlock = { kind: 'text'; text: string }
type TableBlock = { kind: 'table'; headers: string[]; rows: string[][] }
type PaperBlock = TextBlock | TableBlock

function cells(line: string) {
  return line.split('|').map((cell) => cell.trim())
}

function isRow(line: string) {
  return line.includes(' | ') && cells(line).length > 1
}

function paperBlocks(content: string): PaperBlock[] {
  const lines = content.replace(/\r\n/g, '\n').split('\n')
  const blocks: PaperBlock[] = []
  let prose: string[] = []

  function flushProse() {
    const text = prose.join('\n').trim()
    if (text) blocks.push({ kind: 'text', text })
    prose = []
  }

  let index = 0
  while (index < lines.length) {
    if (!isRow(lines[index])) {
      prose.push(lines[index])
      index += 1
      continue
    }

    const headers = cells(lines[index])
    const rows: string[][] = []
    let cursor = index + 1
    while (cursor < lines.length) {
      if (lines[cursor].trim() === '') {
        let ahead = cursor + 1
        while (ahead < lines.length && lines[ahead].trim() === '') ahead += 1
        if (ahead < lines.length && isRow(lines[ahead]) && cells(lines[ahead]).length === headers.length) {
          cursor = ahead
          continue
        }
        break
      }
      if (!isRow(lines[cursor]) || cells(lines[cursor]).length !== headers.length) break
      rows.push(cells(lines[cursor]))
      cursor += 1
    }

    if (rows.length === 0) {
      prose.push(lines[index])
      index += 1
      continue
    }

    flushProse()
    blocks.push({ kind: 'table', headers, rows })
    index = cursor
  }

  flushProse()
  return blocks
}

export function PaperBody({ content }: { content: string }) {
  return (
    <div className="paper-body">
      {paperBlocks(content).map((block, index) =>
        block.kind === 'text' ? (
          <p key={index} className="paper">{block.text}</p>
        ) : (
          <div key={index} className="register">
            <table>
              <thead>
                <tr>
                  {block.headers.map((header) => (
                    <th key={header} scope="col">{header}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {block.rows.map((row) => (
                  <tr key={row.join('|')}>
                    {row.map((cell, cellIndex) => (
                      <td key={`${row[0]}-${cellIndex}`}>{cell}</td>
                    ))}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ),
      )}
    </div>
  )
}
