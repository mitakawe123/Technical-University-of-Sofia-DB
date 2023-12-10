﻿using System.Text;
using DataStructures;
using DMS.Constants;
using DMS.DataPages;
using DMS.Extensions;
using DMS.Shared;
using DMS.Utils;

namespace DMS.Commands
{
    //here will lay the logic for the logic operators such as WHERE, AND, NOT, OR, ORDER BY, DISTINCT, JOIN
    //The purpose of this class is only filtration  
    public static class LogicalOperators
    {
        private static readonly DKList<string> Operators = new();
        private static DKList<string> _operations = new();

        public static void Parse(
            ref IReadOnlyList<char[]> allData,
            DKList<Column> selectedColumns,
            IReadOnlyList<Column> allColumnsForTable,
            ReadOnlySpan<char> logicalOperator,
            int colCount)
        {
            if (logicalOperator.IsEmpty)
                return;

            string operatorStr = new string(logicalOperator).CustomTrim();

            string[] tokens = operatorStr.CustomSplit(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < tokens.Length; i++)
            {
                string token = tokens[i];
                switch (token)
                {
                    case "order":
                        if (tokens.CustomContains("by"))
                            Operators.Add("order by");
                        break;
                    case "where":
                        Operators.Add("where");
                        break;
                    case "and":
                        Operators.Add("and");
                        break;
                    case "or":
                        Operators.Add("or");
                        break;
                    case "not":
                        Operators.Add("not");
                        break;
                    case "join":
                        Operators.Add("join");
                        break;
                    case "distinct":
                        Operators.Add("distinct");
                        break;
                }
            }

            _operations = HelperMethods.SplitSqlQuery(logicalOperator);

            ExecuteConditionQuery(ref allData, allColumnsForTable, selectedColumns, colCount);
        }

        private static void ExecuteConditionQuery(
            ref IReadOnlyList<char[]> allData,
            IReadOnlyList<Column> allColumnsForTable,
            DKList<Column> selectedColumns,
            int colCount)
        {
            for (int i = 0; i < Operators.Count; i++)
            {
                switch (Operators[i])
                {
                    case "where":
                        WhereCondition(ref allData, colCount, _operations[i]);
                        break;
                    case "order by":
                        OrderByCondition(ref allData, allColumnsForTable, colCount, _operations[i]);
                        break;
                    case "join":
                        JoinCondition(ref allData, selectedColumns, colCount, _operations[i]);
                        break;
                    case "distinct":
                        DistinctCondition(ref allData);
                        break;
                }
            }

            Operators.Clear();
            _operations.Clear();
        }

        #region where clause

        private static void WhereCondition(ref IReadOnlyList<char[]> allData, int colCount, string operation)
        {
            var operatorAndIndex = ParseOperation(operation);
            string op = operatorAndIndex.Item1;
            int operatorIndex = operatorAndIndex.Item2;

            char[] value = GetValueFromOperation(operation, operatorIndex);

            DKList<int> blockIndexes = new();
            for (int i = 0; i < allData.Count; i++)
            {
                if (CompareValues(allData[i], value, op))
                {
                    int blockIndex = i / colCount;
                    blockIndexes.Add(blockIndex);
                }
            }

            if (blockIndexes.Count == 0)
            {
                Console.WriteLine("Value not found");
                allData = new DKList<char[]>();
                return;
            }

            DKList<char[]> result = new();
            foreach (int blockIndex in blockIndexes)
            {
                int blockStartIndex = blockIndex * colCount;
                for (int i = blockStartIndex; i < blockStartIndex + colCount && i < allData.Count; i++)
                    result.Add(allData[i]);
            }

            allData = result;
        }

        public static (string, int) ParseOperation(string operation)
        {
            string[] operators = { ">=", "<=", "!=", "=", ">", "<" };
            Array.Sort(operators, (x, y) => y.Length.CompareTo(x.Length));

            foreach (string op in operators)
            {
                int index = operation.CustomIndexOf(op);
                if (index is not -1)
                    return (op, index + op.Length);
            }
            throw new InvalidOperationException("No valid operator found.");
        }

        public static char[] GetValueFromOperation(string operation, int operatorIndex) => operation[operatorIndex..].CustomTrim().CustomToCharArray();

        public static bool CompareValues(char[] value1, char[] value2, string op)
        {
            //need to check for DATE
            bool isValue1Int = int.TryParse(new string(value1), out int intValue1);
            bool isValue2Int = int.TryParse(new string(value2), out int intValue2);

            if (isValue1Int && isValue2Int)
            {
                return op switch
                {
                    "=" => intValue1 == intValue2,
                    ">" => intValue1 > intValue2,
                    "<" => intValue1 < intValue2,
                    ">=" => intValue1 >= intValue2,
                    "<=" => intValue1 <= intValue2,
                    _ => false
                };
            }

            // If either value is not an integer, fall back to string comparison
            int comparison = string.CompareOrdinal(new string(value1), new string(value2));
            return op switch
            {
                "=" => comparison == 0,
                _ => false,
            };
        }

        #endregion

        #region distinct clause

        private static void DistinctCondition(ref IReadOnlyList<char[]> allData)
        {
            DKList<char[]> result = new();

            foreach (char[] rowValue in allData)
                if (!result.CustomAny(existingRow => existingRow.SequenceEqual(rowValue)))
                    result.Add(rowValue);

            allData = result;
        }

        #endregion

        #region order by clause
        
        private static void OrderByCondition(
            ref IReadOnlyList<char[]> allData,
            IReadOnlyList<Column> allColumnsForTable,
            int colCount,
            string operation)
        {
            string[] multiColOrdering = operation.CustomSplit(',');
            
            // Map column names to their indices
            DKDictionary<string, int> columnMap = allColumnsForTable.CustomSelect((col, index) => new { col.Name, Index = index })
                .CustomToDictionary(x => new string(x.Name), x => x.Index);

            DKList<(int Index, bool IsAscending)> sortingColumns = new();
            foreach (string orderClause in multiColOrdering)
            {
                string trimmedOrderClause = orderClause.CustomTrim();
                string columnName = trimmedOrderClause.CustomSplit(' ')[0].CustomTrim();
                bool isAsc = !trimmedOrderClause.CustomContains("desc") && !trimmedOrderClause.CustomContains("descending");

                if (!columnMap.TryGetValue(columnName, out int columnIndex))
                {
                    Console.WriteLine($"Invalid column in order by condition {columnName}");
                    return;
                }
                sortingColumns.Add((columnIndex, isAsc));
            }

            DKList<DKList<char[]>> rows = new();
            for (int i = 0; i < allData.Count; i += colCount)
                rows.Add(allData.CustomSkip(i).CustomTake(colCount).CustomToList());

            Comparison<DKList<char[]>> rowComparer = (row1, row2) =>
            {
                foreach ((int index, bool isAscending) in sortingColumns)
                {
                    string value1 = new(row1[index]);
                    string value2 = new(row2[index]);

                    int comparison = string.Compare(value1, value2, StringComparison.Ordinal);
                    if (comparison is not 0)
                        return isAscending ? comparison : -comparison;
                }
                return 0;
            };

            rows.Sort(rowComparer);

            DKList<char[]> sortedData = rows.CustomSelectMany(row => row).CustomToList();
            allData = sortedData.AsReadOnly();
        }

        #endregion

        #region join clause
        
        private static void JoinCondition(
            ref IReadOnlyList<char[]> allDataFromMainTable,
            DKList<Column> selectedColumns,
            int colCount,
            string operation)
        {
            int indexTableToJoin = operation.CustomIndexOf(' ');
            char[] tableToJoin = operation[..indexTableToJoin].CustomToCharArray();

            int indexOnKeyword = operation.CustomIndexOf("on");
            if (indexOnKeyword is -1)
            {
                Console.WriteLine("There is no \'ON\' keyword after the table that will be joined");
                return;
            }
                
            char[]? matchingKey = DataPageManager.TableOffsets.Keys.CustomFirstOrDefault(table => tableToJoin.SequenceEqual(table)) ?? null;

            if (matchingKey is null)
            {
                Console.WriteLine("Wrong table name cannot use join on that table");
                return;
            }

            var joinedTableData = AllDataFromJoinedTable(DataPageManager.TableOffsets[matchingKey], matchingKey);

            string columnToJoin = operation[(indexOnKeyword + 2)..];
            string[] propsToJoin = columnToJoin.CustomSplit('=');

            string[] mainTableJoinProp = propsToJoin[0].CustomTrim().CustomSplit('.');
            string[] joinedTableJoinProp = propsToJoin[1].CustomTrim().CustomSplit('.');

            int mainTableJoinIndex = HelperMethods.FindColumnIndex(mainTableJoinProp[1], selectedColumns);
            int joinedTableJoinIndex = HelperMethods.FindColumnIndex(joinedTableJoinProp[1], selectedColumns);

            DKList<DKList<char[]>> mainTableRows = new();
            for (int i = 0; i < allDataFromMainTable.Count; i += colCount)
                mainTableRows.Add(allDataFromMainTable.CustomSkip(i).CustomTake(colCount).CustomToList());

            DKList<DKList<char[]>> joinedTableRows = new();
            for (int i = 0; i < joinedTableData.allDataFromJoinedTable.Count; i += joinedTableData.colCount)
                joinedTableRows.Add(joinedTableData.allDataFromJoinedTable.CustomSkip(i).CustomTake(joinedTableData.colCount).CustomToList());

            selectedColumns.AddRange(joinedTableData.columnsForJoinedTable);

            JoinRows(out allDataFromMainTable, mainTableRows, joinedTableRows, mainTableJoinIndex, joinedTableJoinIndex);
        }

        private static void JoinRows(
            out IReadOnlyList<char[]> allDataFromMainTable,
            IReadOnlyList<IReadOnlyList<char[]>> mainTableRows,
            IReadOnlyList<IReadOnlyList<char[]>> joinedTableRows,
            int mainTableJoinIndex,
            int joinedTableJoinIndex)
        {
            DKList<DKList<char[]>> resultRows = new();

            foreach (var mainTableRow in mainTableRows)
            {
                foreach (var joinedTableRow in joinedTableRows)
                {
                    if (!mainTableRow[mainTableJoinIndex].SequenceEqual(joinedTableRow[joinedTableJoinIndex]))
                        continue;

                    DKList<char[]> combinedRow = new();

                    combinedRow.AddRange(mainTableRow);

                    foreach (var tableRow in joinedTableRow)
                        combinedRow.Add(tableRow);

                    resultRows.Add(combinedRow);
                }
            }

            allDataFromMainTable = resultRows.CustomSelectMany(row => row).CustomToArray();
        }
       
        private static (IReadOnlyList<char[]> allDataFromJoinedTable, IReadOnlyList<Column> columnsForJoinedTable, int colCount) 
            AllDataFromJoinedTable(long startOfOffsetForJoinedTable, char[] matchingKey)
        {
            using FileStream fs = new(Files.MDF_FILE_NAME, FileMode.Open);
            using BinaryReader reader = new(fs, Encoding.UTF8);

            fs.Seek(DataPageManager.TableOffsets[matchingKey], SeekOrigin.Begin);

            var metadata = SqlCommands.ReadTableMetadata(reader);

            int headerSectionForMainDp = DataPageManager.Metadata + matchingKey.Length;
            fs.Seek(startOfOffsetForJoinedTable + headerSectionForMainDp, SeekOrigin.Begin);

            (headerSectionForMainDp, DKList<Column> columnTypeAndName) = SqlCommands.ReadColumns(reader, headerSectionForMainDp, metadata.columnCount);

            long start = DataPageManager.TableOffsets[matchingKey] + headerSectionForMainDp;
            long end = DataPageManager.TableOffsets[matchingKey] + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer;
            long lengthToRead = end - start;

            fs.Seek(start, SeekOrigin.Begin);

            DKList<char[]> allDataFromJoinedTable = SqlCommands.ReadAllDataForSingleDataPage(lengthToRead, reader);

            fs.Seek(end, SeekOrigin.Begin);
            long pointer = reader.ReadInt64();

            while (pointer != DataPageManager.DefaultBufferForDp)
            {
                fs.Seek(pointer, SeekOrigin.Begin);
                
                reader.ReadUInt64(); //<- hash
                reader.ReadInt32(); //<- free space
                
                start = pointer + sizeof(int);
                end = pointer + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer;
                lengthToRead = end - start;

                allDataFromJoinedTable = allDataFromJoinedTable.Concat(SqlCommands.ReadAllDataForSingleDataPage(lengthToRead, reader)).CustomToList();
                fs.Seek(pointer + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer, SeekOrigin.Begin);
                pointer = reader.ReadInt64();
            }

            allDataFromJoinedTable.RemoveAll(charArray => charArray.Length == 0 || charArray.All(c => c == '\0'));
            return (allDataFromJoinedTable, columnTypeAndName, metadata.columnCount);
        }
        
        #endregion
    }
}
