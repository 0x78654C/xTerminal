"""Build the versioned TermXT PDF from its Markdown manual.

Dependencies: reportlab; add PyMuPDF for --verify page rendering.
Usage: python Documents/build_termxt_manual.py --verify
Local dependencies installed in .codex-build/pdf-packages are also supported.
"""

from __future__ import annotations

import argparse
import html
import re
import textwrap
from pathlib import Path
import sys

DOCUMENTS = Path(__file__).resolve().parent
LOCAL_PACKAGES = DOCUMENTS.parent / '.codex-build' / 'pdf-packages'
if LOCAL_PACKAGES.is_dir():
    sys.path.insert(0, str(LOCAL_PACKAGES))

from reportlab.lib import colors
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import (
    BaseDocTemplate, Frame, PageBreak, PageTemplate, Paragraph, Preformatted, Spacer, Table, TableStyle,
)
from reportlab.platypus.tableofcontents import TableOfContents

INK = colors.HexColor('#192c40')
BLUE = colors.HexColor('#17638c')
MUTED = colors.HexColor('#5c6b79')
PALE = colors.HexColor('#eff4f8')
RULE = colors.HexColor('#d8e2ea')
PAGE_WIDTH, PAGE_HEIGHT = A4
MARGIN = 48
CONTENT_WIDTH = PAGE_WIDTH - MARGIN * 2


def register_fonts():
    windows_fonts = Path('C:/Windows/Fonts')
    if (windows_fonts / 'segoeui.ttf').exists():
        for name, file in [('Body', 'segoeui.ttf'), ('BodyBold', 'segoeuib.ttf'),
                           ('Code', 'consola.ttf'), ('CodeBold', 'consolab.ttf')]:
            pdfmetrics.registerFont(TTFont(name, str(windows_fonts / file)))
        pdfmetrics.registerFontFamily('Body', normal='Body', bold='BodyBold',
                                      italic='Body', boldItalic='BodyBold')
        return 'Body', 'BodyBold', 'Code'
    return 'Helvetica', 'Helvetica-Bold', 'Courier'


BODY, BOLD, CODE = register_fonts()
STYLES = {
    'body': ParagraphStyle('body', fontName=BODY, fontSize=10, leading=14.2,
                           textColor=INK, spaceAfter=8),
    'title': ParagraphStyle('title', fontName=BOLD, fontSize=31, leading=36,
                            textColor=INK, spaceAfter=15),
    'section': ParagraphStyle('section', fontName=BOLD, fontSize=17, leading=21,
                              textColor=BLUE, spaceBefore=17, spaceAfter=10,
                              keepWithNext=True),
    'code': ParagraphStyle('code', fontName=CODE, fontSize=8.5, leading=11.7,
                           textColor=INK),
    'table': ParagraphStyle('table', fontName=BODY, fontSize=9.1, leading=12.5,
                            textColor=INK),
    'table_header': ParagraphStyle('table_header', fontName=BOLD, fontSize=9.1,
                                   leading=12.5, textColor=colors.white),
    'toc': ParagraphStyle('toc', fontName=BODY, fontSize=11, leading=16,
                          textColor=INK, spaceBefore=7),
}


def inline(text):
    # Protect code and link spans before escaping other Markdown text.
    spans = []

    def protect(markup):
        spans.append(markup)
        return f'\x01{len(spans) - 1}\x02'

    text = re.sub(r'`([^`]+)`', lambda m: protect(
        f'<font name="{CODE}" size="9">{html.escape(m[1])}</font>'), text)
    text = re.sub(r'\[([^\]]+)\]\(([^)]+)\)', lambda m: protect(
        f'<a href="{html.escape(m[2], quote=True)}" color="#17638c">'
        f'{html.escape(m[1])}</a>'), text)
    text = html.escape(text)
    text = re.sub(r'\*\*([^*]+)\*\*', r'<b>\1</b>', text)
    # Links can contain a protected code span in their label.
    for _ in range(2):
        text = re.sub(r'\x01(\d+)\x02', lambda m: spans[int(m[1])], text)
    return text


def table_cells(line):
    result, current, code = [], [], False
    for ch in line.strip().strip('|'):
        if ch == '`':
            code = not code
        if ch == '|' and not code:
            result.append(''.join(current).strip())
            current = []
        else:
            current.append(ch)
    result.append(''.join(current).strip())
    return result


def code_block(lines):
    wrapped = []
    # The width is measured using the embedded monospace font, including padding.
    max_chars = int((CONTENT_WIDTH - 24) / pdfmetrics.stringWidth('M', CODE, 8.5))
    for line in lines:
        wrapped.extend(textwrap.wrap(line, max_chars, subsequent_indent='    ',
                                     break_long_words=True, break_on_hyphens=False,
                                     replace_whitespace=False, drop_whitespace=False) or [''])
    block = Table([[Preformatted('\n'.join(wrapped), STYLES['code'])]],
                  colWidths=[CONTENT_WIDTH], hAlign='LEFT')
    block.setStyle(TableStyle([
        ('BACKGROUND', (0, 0), (-1, -1), PALE),
        ('BOX', (0, 0), (-1, -1), 0.5, RULE),
        ('LEFTPADDING', (0, 0), (-1, -1), 12),
        ('RIGHTPADDING', (0, 0), (-1, -1), 12),
        ('TOPPADDING', (0, 0), (-1, -1), 10),
        ('BOTTOMPADDING', (0, 0), (-1, -1), 10),
    ]))
    block.spaceAfter = 12
    return block


class Manual(BaseDocTemplate):
    def afterFlowable(self, flowable):
        if isinstance(flowable, Paragraph) and flowable.style.name == 'section':
            title = flowable.getPlainText()
            if title != 'Contents':
                bookmark = re.sub(r'[^a-z0-9]+', '-', title.lower())
                self.canv.bookmarkPage(bookmark)
                self.canv.addOutlineEntry(title, bookmark, 0)
                self.notify('TOCEntry', (0, title, self.page, bookmark))


def build(source, output, version):
    lines = source.read_text(encoding='utf-8-sig').splitlines()
    story, paragraph = [], []
    index, contents_added = 0, False

    def flush():
        if paragraph:
            story.append(Paragraph(inline(' '.join(paragraph)), STYLES['body']))
            paragraph.clear()

    while index < len(lines):
        line = lines[index]
        if line.startswith('# '):
            flush()
            story.append(Spacer(1, 38))
            story.append(Paragraph('xTerminal / Language reference', ParagraphStyle(
                'eyebrow', parent=STYLES['body'], fontName=BOLD, textColor=BLUE, spaceAfter=20)))
            story.append(Paragraph('TermXT scripting<br/>language user manual', STYLES['title']))
        elif line.startswith('## '):
            flush()
            if line.startswith('## Examples and changes'):
                story.append(PageBreak())
            if not contents_added:
                story.append(Spacer(1, 14))
                story.append(Paragraph('Contents', STYLES['section']))
                toc = TableOfContents()
                toc.levelStyles = [STYLES['toc']]
                toc.dotsMinLevel = 0
                story.append(toc)
                story.append(PageBreak())
                contents_added = True
            story.append(Paragraph(inline(line[3:]), STYLES['section']))
        elif line.startswith('```'):
            flush()
            code = []
            index += 1
            while index < len(lines) and not lines[index].startswith('```'):
                code.append(lines[index])
                index += 1
            story.append(code_block(code))
        elif line.startswith('|'):
            flush()
            rows = []
            while index < len(lines) and lines[index].startswith('|'):
                cells = table_cells(lines[index])
                if not all(re.fullmatch(r':?-+:?', cell) for cell in cells):
                    style = STYLES['table_header'] if not rows else STYLES['table']
                    rows.append([Paragraph(inline(cell), style) for cell in cells])
                index += 1
            index -= 1
            table = Table(rows, colWidths=[CONTENT_WIDTH * 0.31, CONTENT_WIDTH * 0.69],
                          repeatRows=1, hAlign='LEFT')
            table.setStyle(TableStyle([
                ('BACKGROUND', (0, 0), (-1, 0), BLUE),
                ('ROWBACKGROUNDS', (0, 1), (-1, -1), [colors.white, PALE]),
                ('LINEBELOW', (0, 0), (-1, -1), 0.4, RULE),
                ('VALIGN', (0, 0), (-1, -1), 'TOP'),
                ('LEFTPADDING', (0, 0), (-1, -1), 9),
                ('RIGHTPADDING', (0, 0), (-1, -1), 9),
                ('TOPPADDING', (0, 0), (-1, -1), 7),
                ('BOTTOMPADDING', (0, 0), (-1, -1), 7),
            ]))
            table.spaceAfter = 12
            story.append(table)
        elif line.strip():
            paragraph.append(line.strip())
        else:
            flush()
        index += 1
    flush()

    def furniture(canvas, document):
        canvas.saveState()
        canvas.setStrokeColor(RULE)
        canvas.setLineWidth(0.5)
        if document.page > 1:
            canvas.line(MARGIN, PAGE_HEIGHT - 37, PAGE_WIDTH - MARGIN, PAGE_HEIGHT - 37)
            canvas.setFont(BODY, 8)
            canvas.setFillColor(MUTED)
            canvas.drawString(MARGIN, PAGE_HEIGHT - 27, 'TermXT scripting language')
            canvas.drawRightString(PAGE_WIDTH - MARGIN, PAGE_HEIGHT - 27, f'Version {version}')
        canvas.line(MARGIN, 37, PAGE_WIDTH - MARGIN, 37)
        canvas.setFont(BODY, 8)
        canvas.setFillColor(MUTED)
        canvas.drawString(MARGIN, 24, 'xTerminal  /  User manual')
        canvas.drawRightString(PAGE_WIDTH - MARGIN, 24, str(document.page))
        canvas.restoreState()

    doc = Manual(str(output), pagesize=A4, rightMargin=MARGIN, leftMargin=MARGIN,
                 topMargin=53, bottomMargin=52, title=f'TermXT user manual {version}',
                 author='xTerminal', subject='TermXT scripting language reference',
                 pageCompression=1)
    frame = Frame(MARGIN, 52, CONTENT_WIDTH, PAGE_HEIGHT - 105, id='content')
    doc.addPageTemplates(PageTemplate(id='manual', frames=[frame], onPage=furniture))
    doc.multiBuild(story)


def verify(output, version):
    import pymupdf

    review = DOCUMENTS.parent / '.codex-build' / 'termxt-pdf-review'
    review.mkdir(parents=True, exist_ok=True)
    original = DOCUMENTS / 'TermXT_Scripting_Language_User_Manual_v1.0.0.pdf'
    if original.exists():
        with pymupdf.open(original) as old:
            print(f'Older PDF: {len(old)} pages; {old[0].get_text()[:160]!r}')
            old[0].get_pixmap(matrix=pymupdf.Matrix(1, 1)).save(str(review / 'original-cover.png'))
    with pymupdf.open(output) as pdf:
        full_text = '\n'.join(page.get_text() for page in pdf)
        for required in [version, '{argc}', 'Conditions', 'Functions', '128', 'language_features.xt']:
            if required not in full_text:
                raise RuntimeError(f'Missing expected PDF text: {required}')
        for page in pdf:
            if not page.get_text().strip():
                raise RuntimeError(f'Blank page: {page.number + 1}')
            if page.number > 0 and f'Version {version}' not in page.get_text():
                raise RuntimeError(f'Missing running header: page {page.number + 1}')
            for block in page.get_text('dict')['blocks']:
                if block['type'] != 0:
                    continue
                for line in block['lines']:
                    x0, y0, x1, y1 = line['bbox']
                    if x0 < 20 or y0 < 12 or x1 > page.rect.width - 20 or y1 > page.rect.height - 12:
                        raise RuntimeError(f'Text outside page margins: page {page.number + 1}, {line["bbox"]}')
            page.get_pixmap(matrix=pymupdf.Matrix(1.4, 1.4)).save(str(review / f'page-{page.number + 1:02}.png'))
        print(f'Verified {len(pdf)} pages. Rendered pages: {review}')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--verify', action='store_true')
    args = parser.parse_args()
    source = DOCUMENTS / 'TermXT_Scripting_Language_User_Manual.md'
    version = re.search(r'Current version: \*\*([\d.]+)\*\*', source.read_text(encoding='utf-8'))[1]
    output = DOCUMENTS / f'TermXT_Scripting_Language_User_Manual_v{version}.pdf'
    build(source, output, version)
    print(f'Created {output} ({output.stat().st_size:,} bytes)')
    if args.verify:
        verify(output, version)


if __name__ == '__main__':
    main()
