DateTime start = new DateTime(2022, 11, 07, 00, 00, 01);
DateTime end = new DateTime(2022, 11, 07, 23, 59, 59);
TimeSpan different = end - start;

//start += different / 2;
//Console.WriteLine(start);

DateTime dend;

for (DateTime ds = start; ds < end; ds += different / 2)
{
    dend = ds + different / 2;
    Console.WriteLine($"{ds} - {dend}");
}