echo counts scdt
sqlite3 results.sqlite < counts_scdt.sql
echo counts scd
sqlite3 results.sqlite < counts_scd.sql
echo counts sc
sqlite3 results.sqlite < counts_sc.sql
echo energy stats
sqlite3 results.sqlite < energy_stat.sql