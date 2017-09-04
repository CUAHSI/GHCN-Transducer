using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text;


public class BinaryFileData
{
    public DateTime BeginDateTime { get; set; }

    public float[] Data { get; set; }
}

public class GetValuesResult
{
    public GetValuesResult(int nVals)
    {
        _vals = new double[nVals];
    }
    private double[] _vals;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public double[] Vals { get { return _vals; } }
}

public class DateRange
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public bool HasValues { get; set; }
}

/// <summary>
/// Summary description for BinaryFileHelper
/// </summary>
public class BinaryFileHelper
{

    public static string GetDataDirectory()
    {
        return ConfigurationManager.ConnectionStrings["FileConnection1"].ConnectionString;
    }
    
    public static string GetBinaryFileName(int siteId, string varCode, string timeStep)
    {
        //construct fileName
        string baseDir = GetDataDirectory();
        string stepName = "h";
        string stationCode = siteId.ToString("D4");
        float noDataValue = -9999.0f;

        string file = string.Format(@"{0}\{1}\{0}_{1}_{2}.dat", timeStep, varCode, stationCode);
        string fileName = Path.Combine(baseDir, file);
        return fileName;
    }
    
    /// <summary>
    /// Reads the time range (start time, end time) from the binary file
    /// </summary>
    /// <param name="fileName">file name</param>
    /// <param name="timeStep">time step. use "d" for day or "h" for hour</param>
    /// <returns>an array with two items. First item is start date.
    /// second item is end date.</returns>
    public static DateRange BinaryFileDateRange(string fileName, string timeStep)
    {
        int SIZEOF_FLOAT = 4;
        int SIZEOF_LONG = 8;

        try
        {
            using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                //read the startDate
                byte[] startDateBytes = new byte[SIZEOF_LONG];
                stream.Read(startDateBytes, 0, startDateBytes.Length);
                long[] startDateBinary = new long[1];
                Buffer.BlockCopy(startDateBytes, 0, startDateBinary, 0, SIZEOF_LONG);
                DateTime startDateFromFile = DateTime.FromBinary(startDateBinary[0]);

                DateTime endDateFromFile;
                int numStepsInFile = (int)((stream.Length - SIZEOF_LONG) / SIZEOF_FLOAT);
                if (timeStep == "h")
                {

                    endDateFromFile = startDateFromFile.AddHours(numStepsInFile);
                }
                else
                {
                    endDateFromFile = startDateFromFile.AddDays(numStepsInFile);
                }

                DateRange result = new DateRange();
                result.Start = startDateFromFile;
                result.End = endDateFromFile;
                result.HasValues = true;
                return result;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return null;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="fileName">the binary file name</param>
    /// <param name="startTime">start time</param>
    /// <param name="endTime"></param>
    /// <param name="timeStep">"h" for hour or "d" for day</param>
    /// <param name="includeNA">true if NA values should be included</param>
    /// <param name="observations">the output list of observations</param>
    public static BinaryFileData ReadBinaryFile(string fileName, DateTime startTime, DateTime endTime, string timeStep,
        bool includeNA)
    {
        if (timeStep == "d")
        {
            return ReadBinaryFileDaily(fileName, startTime, endTime, includeNA);
        }
        else
        {
            return ReadBinaryFileHourly(fileName, startTime, endTime, includeNA);
        }
    }

    public static BinaryFileData ReadBinaryFileHourly(string fileName, DateTime startTime, DateTime endTime,
        bool includeNA)
    {
        int SIZEOF_FLOAT = 4;
        int SIZEOF_LONG = 8;

        try
        {
            using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                //read the startDate
                byte[] startDateBytes = new byte[SIZEOF_LONG];
                stream.Read(startDateBytes, 0, startDateBytes.Length);
                long[] startDateBinary = new long[1];
                Buffer.BlockCopy(startDateBytes, 0, startDateBinary, 0, SIZEOF_LONG);
                DateTime startDateFromFile = DateTime.FromBinary(startDateBinary[0]);

                //check start time
                if (startTime < startDateFromFile)
                {
                    startTime = startDateFromFile;
                }

                //find position of query start time
                int startTimePositionHours = (int)((startTime - startDateFromFile).TotalHours);
                if (startTimePositionHours < 0)
                    return null;
                int numHoursInFile = (int)((stream.Length - SIZEOF_LONG) / SIZEOF_FLOAT);
                DateTime endDateFromFile = startDateFromFile.AddHours(numHoursInFile);

                if (endTime < startDateFromFile)
                    return null;
                if (startTime > endDateFromFile)
                    return null;

                long startTimePositionInBytes = SIZEOF_LONG + startTimePositionHours * SIZEOF_FLOAT;
                int numHoursStartEnd = (int)((endTime - startTime).TotalHours);
                long numBytesStartEnd = numHoursStartEnd * SIZEOF_FLOAT;
                if (startTimePositionInBytes + numBytesStartEnd > stream.Length)
                {
                    numBytesStartEnd = stream.Length - startTimePositionInBytes;
                    numHoursStartEnd = (int)(numBytesStartEnd / SIZEOF_FLOAT);
                }
                long endTimePositionInBytes = startTimePositionInBytes + numBytesStartEnd;

                byte[] resultBytes = new byte[numBytesStartEnd];


                stream.Seek(startTimePositionInBytes, SeekOrigin.Begin);
                stream.Read(resultBytes, 0, resultBytes.Length);

                float[] result = new float[numHoursStartEnd];
                Buffer.BlockCopy(resultBytes, 0, result, 0, resultBytes.Length);

                BinaryFileData res = new BinaryFileData();
                res.BeginDateTime = startTime;
                res.Data = result;
                return res;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return null;
        }
    }

    public static BinaryFileData ReadBinaryFileDaily(string fileName, DateTime startTime, DateTime endTime,
        bool includeNA)
    {
        int SIZEOF_FLOAT = 4;
        int SIZEOF_LONG = 8;

        try
        {
            using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                //read the startDate
                byte[] startDateBytes = new byte[SIZEOF_LONG];
                stream.Read(startDateBytes, 0, startDateBytes.Length);
                long[] startDateBinary = new long[1];
                Buffer.BlockCopy(startDateBytes, 0, startDateBinary, 0, SIZEOF_LONG);
                DateTime startDateFromFile = DateTime.FromBinary(startDateBinary[0]);

                //check start time
                if (startTime < startDateFromFile)
                {
                    startTime = startDateFromFile;
                }

                //find position of query start time
                int startTimePositionDays = (int)((startTime - startDateFromFile).TotalDays);
                if (startTimePositionDays < 0)
                    return null;
                int numDaysInFile = (int)((stream.Length - SIZEOF_LONG) / SIZEOF_FLOAT);
                DateTime endDateFromFile = startDateFromFile.AddDays(numDaysInFile);

                if (endTime < startDateFromFile)
                    return null;
                if (startTime > endDateFromFile)
                    return null;

                long startTimePositionInBytes = SIZEOF_LONG + startTimePositionDays * SIZEOF_FLOAT;
                int numDaysStartEnd = (int)((endTime - startTime).TotalDays);
                long numBytesStartEnd = numDaysStartEnd * SIZEOF_FLOAT;
                if (startTimePositionInBytes + numBytesStartEnd > stream.Length)
                {
                    numBytesStartEnd = stream.Length - startTimePositionInBytes;
                    numDaysStartEnd = (int)(numBytesStartEnd / SIZEOF_FLOAT);
                }
                long endTimePositionInBytes = startTimePositionInBytes + numBytesStartEnd;

                byte[] resultBytes = new byte[numBytesStartEnd];


                stream.Seek(startTimePositionInBytes, SeekOrigin.Begin);
                stream.Read(resultBytes, 0, resultBytes.Length);

                float[] result = new float[numDaysStartEnd];
                Buffer.BlockCopy(resultBytes, 0, result, 0, resultBytes.Length);

                BinaryFileData res = new BinaryFileData();
                res.BeginDateTime = startDateFromFile;
                res.Data = result;
                return res;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return null;
        }
    }
}