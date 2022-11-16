sqlplot-tools import-data -D sqlite:results.sqlite simulation results.txt
echo "Resetting the preprocessed Tables and Views"
Rscript after_import.r
# sqlite3 results.sqlite < after_import.sql