using System;
using System.Collections.Generic;
using System.Text;

namespace USC.GISResearchLab.Geocoding.Core.OutputData.ResultCodes
{

    public enum GeocodeQueryResultCodes 
    { 
        Unknown, 
        Sucess, 
        ProcessError,
        InputError
    };

    public enum GeocodeQueryInputErrorResultCodes
    {
        None,
        Unknown,
        MissingApiKey,
        ApiKeyInvalid
    };


    public class GeocodeQueryResultCodeManager
    {

        public const string GEOCODE_QUERY_RESULT_CODE_UNKNOWN = "Unknown";
        public const string GEOCODE_QUERY_RESULT_CODE_SUCCESS = "Sucess";
        public const string GEOCODE_QUERY_RESULT_CODE_PROCESSERROR = "ProcessError";
        public const string GEOCODE_QUERY_RESULT_CODE_INPUTERROR = "InputError";


        public const string GEOCODE_INPUT_ERROR_RESULT_CODE_UNKNOWN = "Unknown";
        public const string GEOCODE_INPUT_ERROR_RESULT_CODE_NONE = "None";
        public const string GEOCODE_INPUT_ERROR_RESULT_CODE_MISSINGAPIKEY = "MissingApiKey";
        public const string GEOCODE_INPUT_ERROR_RESULT_CODE_APIKEYINVALID = "ApiKeyInvalid";

        public static string GetResultCodeString(GeocodeQueryResultCodes code)
        {
            string ret = GEOCODE_QUERY_RESULT_CODE_UNKNOWN;
            switch (code)
            {
                case GeocodeQueryResultCodes.InputError:
                    ret = GEOCODE_QUERY_RESULT_CODE_INPUTERROR;
                    break;
                case GeocodeQueryResultCodes.ProcessError:
                    ret = GEOCODE_QUERY_RESULT_CODE_PROCESSERROR;
                    break;
                case GeocodeQueryResultCodes.Sucess:
                    ret = GEOCODE_QUERY_RESULT_CODE_SUCCESS;
                    break;
                case GeocodeQueryResultCodes.Unknown:
                    ret = GEOCODE_QUERY_RESULT_CODE_UNKNOWN;
                    break;
                default:
                    throw new Exception("Unexpected GeocodeQueryResultCodes: " + code);
            }
            return ret;
        }

        public static string GetInputErrorCodeString(GeocodeQueryInputErrorResultCodes code)
        {
            string ret = GEOCODE_QUERY_RESULT_CODE_UNKNOWN;
            switch (code)
            {
                case GeocodeQueryInputErrorResultCodes.ApiKeyInvalid:
                    ret = GEOCODE_INPUT_ERROR_RESULT_CODE_APIKEYINVALID;
                    break;
                case GeocodeQueryInputErrorResultCodes.MissingApiKey:
                    ret = GEOCODE_INPUT_ERROR_RESULT_CODE_MISSINGAPIKEY;
                    break;
                case GeocodeQueryInputErrorResultCodes.None:
                    ret = GEOCODE_INPUT_ERROR_RESULT_CODE_NONE;
                    break;
                case GeocodeQueryInputErrorResultCodes.Unknown:
                    ret = GEOCODE_INPUT_ERROR_RESULT_CODE_UNKNOWN;
                    break;
                default:
                    throw new Exception("Unexpected GeocodeQueryInputErrorResultCodes: " + code);
            }
            return ret;
        }

    }
}
