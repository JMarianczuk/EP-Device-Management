library(ggplot2)
library(optparse)
library(RSQLite)

con <- dbConnect(SQLite(), "results.sqlite")
someRes <- dbExecute(con, "PRAGMA case_sensitive_like=ON;")

query <- paste("select", "*, count(*) as size", 'from simulation where success="True" group by strategy, probability, packetSize')

print(query)

res <- dbGetQuery(con, query)
numValue <- function(text) {
    onlyNumber <- substring(text, first=1, last=nchar(text) - 2)
    numeric <- as.numeric(onlyNumber)
    numeric
}

thisplot <- ggplot(res, aes(x = reorder(probability, numValue(probability)), y = packetSize, colour=strategy, size=size)) +
    geom_jitter() +
    labs(x = "probability")

ggsave("plot.pdf", thisplot, width=25, height=10, units="cm")
