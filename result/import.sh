sqlplot-tools import-data -D sqlite:results.sqlite simulation results.txt
echo "Enhancing the database"
Rscript after_import.r
# sqlite3 results.sqlite < after_import.sql