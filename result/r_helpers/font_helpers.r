library(showtext)

load_fonts <- function() {
    font_paths(paste0(Sys.getenv("LOCALAPPDATA"), "\\Microsoft\\Windows\\Fonts"))
    font_add(
        "JetBrains Mono",
        regular = "JetBrainsMono-Regular.ttf",
        bold = "JetBrainsMono-Bold.ttf",
        italic = "JetBrainsMono-Italic.ttf",
        bolditalic = "JetBrainsMono-BoldItalic.ttf")
    font_add(
        "Lucida Console",
        regular = "lucon.ttf")
}