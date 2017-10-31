using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Drawing;
using USC.GISResearchLab.Geocoding.Core.Configurations;
using USC.GISResearchLab.Geocoding.Core.Metadata;
using USC.GISResearchLab.Common.Core.Geocoders.ReferenceDatasets.Sources.Interfaces;
using USC.GISResearchLab.Geocoding.Core.ReferenceDatasets.Sources.Implementations;
using USC.GISResearchLab.Common.Core.Databases;
using USC.GISResearchLab.Geocoding.Core.Metadata.ReferenceSources;
using USC.GISResearchLab.Geocoding.Core.Metadata.FeatureMatchingResults;
using USC.GISResearchLab.Geocoding.Core.Metadata.Qualities;
using USC.GISResearchLab.Core.WebServices.ResultCodes;
using System.Reflection;
using USC.GISResearchLab.AddressProcessing.Core.Standardizing.StandardizedAddresses.Lines.LastLines;
using Tamu.GeoInnovation.Geocoding.Core.Algorithms.PenaltyScoring;
using USC.GISResearchLab.Geocoding.Core.Algorithms.FeatureMatchScorers.MatchScoreResults;


namespace USC.GISResearchLab.Geocoding.Core.OutputData
{
    public class GeocodeResultSet
    {

        #region Properties
        public string Resultstring { get; set; }
        public QueryStatusCodes QueryStatusCodes { get; set; }
        public GeocodeCollection GeocodeCollection { get; set; }
        public GeocodeQualityType GeocodeQualityType { get; set; }

        public GeocodeStatistics Statistics { get; set; }
        public TimeSpan TimeTaken { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string TransactionId { get; set; }
        public string RecordId { get; set; }
        public string GeocoderName { get; set; }
        public string MicroMatchStatus { get; set; }
        public PenaltyCodeResult PenaltyCodeResult { get; set; }
        public string PenaltyCode { get; set; }

        public int parcelMatches = 0;
        public int streetMatches = 0;
        public FeatureMatchingResultType FeatureMatchingResultType { get; set; }
        public int FeatureMatchingResultCount { get; set; }

        public string ErrorMessage { get; set; }
        public bool ExceptionOccurred { get; set; }

        [XmlIgnore]
        public Exception Exception { get; set; }

        public bool Valid
        {
            get
            {
                bool ret = false;
                if (GeocodeCollection.Geocodes.Count > 0)
                {
                    for (int i = 0; i < GeocodeCollection.Geocodes.Count; i++)
                    {
                        IGeocode geocode = GeocodeCollection.Geocodes[i];
                        if (geocode.Valid == true)
                        {
                            ret = true;
                            break;
                        }
                    }
                }

                return ret;
            }
        }

        public IGeocode BestGeocodeHierarchyFeatureType
        {
            get
            {
                IGeocode ret = null;
                if (GeocodeCollection != null)
                {
                    if (GeocodeCollection.Geocodes.Count > 0)
                    {
                        for (int i = 0; i < GeocodeCollection.Geocodes.Count; i++)
                        {
                            IGeocode geocode = GeocodeCollection.Geocodes[i];
                            if (geocode != null)
                            {
                                if (geocode.Valid == true && geocode.GeocodedError.ErrorBounds >= 0)
                                {
                                    ret = geocode;
                                    break;
                                }
                            }
                            else
                            {
                                string here = "";
                            }
                        }

                        // if the ret is null, none of the geocodes were valid - return the first one that was attempted
                        if (ret == null)
                        {
                            for (int i = 0; i < GeocodeCollection.Geocodes.Count; i++)
                            {
                                IGeocode geocode = GeocodeCollection.Geocodes[i];
                                if (geocode != null)
                                {
                                    if (geocode.Attempted)
                                    {
                                        ret = geocode;
                                        break;
                                    }
                                }
                                else
                                {
                                    string here = "";
                                }
                            }
                        }

                        // if the ret is still null, none of the geocodes were even attempted - return the first one
                        if (ret == null)
                        {
                            IGeocode geocode = GeocodeCollection.Geocodes[0];
                            if (geocode != null)
                            {
                                ret = GeocodeCollection.Geocodes[0];
                            }
                            else
                            {
                                string here = "";
                            }
                        }
                    }
                }
                else
                {
                    ret = new Geocode(2.94);
                }


                if (ret == null)
                {
                    ret = new Geocode(2.94);
                }

                if (ret != null)
                {

                    ret.TotalTimeTaken = TimeTaken;

                    if (Statistics != null)
                    {
                        ret.CompleteProcessStatistics = Statistics;
                    }
                    else
                    {
                        string here = "";
                    }
                }
                else
                {
                    string here = "";
                }

                return ret;
            }
        }

        public IGeocode BestGeocodeHierarchyUncertainty
        {
            get
            {
                IGeocode ret = null;
                IGeocode bestHierarchy = BestGeocodeHierarchyFeatureType;
                if (GeocodeCollection.Geocodes.Count > 0)
                {
                    double leastError = Double.MaxValue;
                    foreach (IGeocode geocode in GeocodeCollection.Geocodes)
                    {
                        if (geocode.Valid == true)
                        {
                            if (geocode.GeocodedError.ErrorBounds >= 0 && geocode.GeocodedError.ErrorBounds < leastError)
                            {
                                if (String.Compare(bestHierarchy.SourceType, geocode.SourceType, true) != 0) // don't compare the id's on the same geocode to itself
                                {
                                    string bestHierarchyId = bestHierarchy.MatchedFeature.PrimaryIdValue;
                                    string bestUncertaintyId = geocode.MatchedFeature.PrimaryIdValue;

                                    if (String.Compare(bestHierarchyId, bestUncertaintyId, true) == 0) // if the uncertainty and hierarchy found the same feature, go with the hierarchy (choose tiger over USPS tiger/zip)
                                    {
                                        // do nothing, will revert to hierarchy
                                    }
                                    // removed DG 2015-06-09 
                                    // TODO test to see if still needed
                                    //else if (bestHierarchy.FM_Result.ReferenceDatasetStatistics.ReferenceSourceQueryResultSet.SawCandidate(bestUncertaintyId)) // if the hierarchy found and rejected the uncertainty, go with the hierarchy (choose tiger over USPS tiger/zip)
                                    //{
                                    //    // do nothing, will revert to hierarchy
                                    //}
                                    else
                                    {
                                        leastError = geocode.GeocodedError.ErrorBounds;
                                        ret = geocode;
                                    }
                                }
                                else
                                {
                                    leastError = geocode.GeocodedError.ErrorBounds;
                                    ret = geocode;
                                }
                            }
                        }
                    }
                }

                if (ret == null)
                {
                    ret = bestHierarchy;
                }

                ret.TotalTimeTaken = TimeTaken;
                ret.CompleteProcessStatistics = Statistics;

                return ret;
            }
        }

        //PAYTON:MULTITHREADING Added this method due to HierarchyfeatureType no longer being sorted correctly due to how the threads return query results
        public IGeocode BestGeocodeHierarchyConfidence
        {
            get
            {

                try
                {

                    IGeocode ret = null;

                    List<IGeocode> tempList = new List<IGeocode>();
                    List<IGeocode> geocodes = new List<IGeocode>();
                    if (GeocodeCollection.Geocodes.Count > 0)
                    {
                        try
                        {
                            geocodes = GeocodeCollection.GetValidGeocodes();

                        }
                        catch (Exception e)
                        {
                            throw new Exception("Error in BestGeocodeHierarchyConfidence: getValidGeocodes " + e.InnerException + " and msg: " + e.Message);
                        }
                        try
                        {
                            //PAYTON:MULTITHREADING-sort I'm concerned about the overhead of the sort here in batch processing - should be using sortbyconfidence()                            
                            tempList = geocodes.OrderBy(d => d.NAACCRGISCoordinateQualityCode).ToList();

                        }
                        catch (Exception e)
                        {
                            throw new Exception("Error in OrderBy(d => d.NAACCRGISCoordinateQualityCode " + e.InnerException + " and msg: " + e.Message);
                        }

                        //This is nothing but a placeholder. It's an ok sort but we need to determine here how to determine <accept-reject-review> 


                    }
                    if (tempList.Count > 0)
                    {
                        foreach (IGeocode geocode in tempList)
                        {
                            if (geocode != null)
                            {
                                if (geocode.Valid == true && geocode.GeocodedError.ErrorBounds >= 0)
                                {
                                    ret = geocode;
                                    break;
                                }
                            }
                            else
                            {
                                string here = "";
                            }
                        }
                    }

                    if (ret == null)
                    {
                        ret = new Geocode(2.94);
                    }

                    ret.TotalTimeTaken = TimeTaken;
                    ret.CompleteProcessStatistics = Statistics;

                    return ret;
                }
                catch (Exception e)
                {
                    throw new Exception("Error in MAIN BestGeocodeHierarchyConfidence " + e.InnerException + " and msg: " + e.Message);
                }
            }
        }


        #endregion

        public GeocodeResultSet()
        {
            Statistics = new GeocodeStatistics();
            GeocodeCollection = new GeocodeCollection();
            QueryStatusCodes = QueryStatusCodes.Unknown;
            FeatureMatchingResultType = FeatureMatchingResultType.Unknown;
        }

        public void AddGeocode(IGeocode geocode)
        {
            GeocodeCollection.Geocodes.Add(geocode);
        }

        public void AddGeocodeList(List<IGeocode> geocodes)
        {
            try {
                GeocodeCollection.Geocodes.AddRange(geocodes);
            }
            catch (Exception e)
            {
                string excp = e.Message;
            }
        }

        public override string ToString()
        {
            return ToString(false);
        }

        public string ToString(bool verbose)
        {
            StringBuilder ret = new StringBuilder();
            try
            {
                if (!verbose)
                {
                    ret.Append(GeocodeCollection);
                }
                else
                {
                    ret.AppendFormat("BestGeocodeHierarchyUncertainty: {0}", BestGeocodeHierarchyUncertainty);
                    ret.AppendLine();
                    ret.AppendFormat("BestGeocodeHierarchyFeatureType: {0}", BestGeocodeHierarchyFeatureType);
                    ret.AppendLine();
                    ret.AppendFormat("GeocodeCollection: {0}", GeocodeCollection);
                    ret.AppendLine();
                    ret.AppendFormat("Statistics: {0}", Statistics);
                }
            }
            catch (Exception e)
            {
                throw new Exception("An error occurred converting GeocodeResult.ToString()", e);
            }


            return ret.ToString();
        }
        public List<IGeocode> SortByHierarchyUncertainty()
        {
            List<IGeocode> ret = new List<IGeocode>();
            if (GeocodeCollection.Geocodes.Count > 0)
            {
                List<IGeocode> geocodes = GeocodeCollection.GetValidGeocodes();
                ret = geocodes.OrderBy(x => x.GeocodedError.ErrorBounds).ToList();
            }
            return ret;
        }


        public List<IGeocode> SortByHierarchyFeatureType()
        {
            return GeocodeCollection.GetValidGeocodes();
        }

        //PAYTON:MULTITHREADING-SORT Added this as part of multithreading setup - will have every source query here to sort
        public List<IGeocode> SortByConfidence()
        {
            //return GeocodeCollection.GetValidGeocodes();
            List<IGeocode> ret = new List<IGeocode>();
            if (GeocodeCollection.Geocodes.Count > 0)
            {
                List<IGeocode> geocodes = GeocodeCollection.GetValidGeocodes();

                //Ideally we want to use the default order of preferred references here to get the best geocode                 

                //IFeatureSource[] referenceSources = BuildReferenceSources(geocoderConfiguration, geocoderConfiguration.OutputHierarchyConfiguration.FeatureMatchingHierarchyOrdering);

                //DefaultOrderedReferenceSourceTypes

                //This is nothing but a placeholder. It's an ok sort but we need to determine here how to determine <accept-reject-review> 
                ret = geocodes.OrderBy(d => d.NAACCRGISCoordinateQualityCode).ThenByDescending(d => d.MatchScore).ToList();
                this.GeocodeCollection.Geocodes.OrderBy(d => d.NAACCRGISCoordinateQualityCode).ThenByDescending(d => d.MatchScore);

                //PAYTON:MULTITHREADING-sort at this point we have it sorted based on Preferred reference. We still need to select the 'best' geocode
                //PAYTON:v4.03 Updating to return lower level street match if the zipcode matches input zipcode
                if (geocodes.Count > 0)
                {
                    if (ret[0].MatchedFeatureAddress.ZIP != ret[0].InputAddress.ZIP)
                    {
                        double score = ret[0].MatchScore;
                        int i = 0;
                        try
                        {
                            foreach (IGeocode g in ret)
                            {
                                if (!object.ReferenceEquals(null, g))
                                {
                                    //Payton:v4.04 added logic to account for reference sources not having a zip but matchscore indicates good match
                                    if ((g.MatchedFeatureAddress.ZIP == g.InputAddress.ZIP && g.MatchScore > score) || g.MatchedFeatureAddress.ZIP == "" && g.MatchScore>90)
                                    //if (g.MatchedFeatureAddress.ZIP == g.InputAddress.ZIP && g.MatchScore > score)
                                    {
                                        ret.RemoveAt(i);
                                        ret.Insert(0, g);
                                        break;
                                    }
                                }
                                i++;
                            }
                        }
                        catch (Exception e)
                        {
                            throw new Exception("BOO in checkForBetterMatch " + e.InnerException + " and msg: " + e.Message + "and record is: " + Convert.ToString(i) + "and value1 is: " + geocodes[i - 1].ToString() + "and value2 is: " + geocodes[i].ToString() + "and value2 is: " + geocodes[i + 1].ToString());
                        }
                    }
                    else //if first address zip is correct then there is no need to test remaining geocodes for better match
                    {
                        ret = geocodes;
                    }

                }
                //if no valid geocodes exist ret needs to add top to be unmatchable
                else
                {
                    ret.Add(GeocodeCollection.Geocodes[0]);
                }
            }
            GeocodeCollection.Geocodes = ret;
            return ret;
        }

        public List<IGeocode> SortByConfidence(GeocoderConfiguration geocoderConfiguration)
        {
            //return GeocodeCollection.GetValidGeocodes();
            List<IGeocode> ret = new List<IGeocode>();
            if (GeocodeCollection.Geocodes.Count > 0)
            {
                List<IGeocode> geocodes = GeocodeCollection.GetValidGeocodes();

                var geoRefList = geocodes.ToList(); //this is the current geocode order
                var ReferenceDataSources = geocoderConfiguration.ReferenceDatasetConfiguration.ReferenceDataSources; //this is the preferred reference order

                foreach (var reference in ReferenceDataSources)
                {

                    PropertyInfo pi = reference.GetType().GetProperty("Name");
                    string refTxt = (String)(pi.GetValue(reference, null));
                    foreach (var geoRef in geoRefList)
                    {
                        if (refTxt.Contains(geoRef.SourceType))
                        {
                            ret.Add(geoRef);
                        }
                    }
                }
                //PAYTON:MULTITHREADING-sort at this point we have it sorted based on Preferred reference. We still need to select the 'best' geocode
                //PAYTON:v4.03 Updating to return lower level street match if the zipcode matches input zipcode
                if (geocodes.Count > 0)
                {
                    if (ret[0].MatchedFeatureAddress.ZIP != ret[0].InputAddress.ZIP)
                    {
                        double score = ret[0].MatchScore;
                        int i = 0;
                        try
                        {
                            foreach (IGeocode g in ret)
                            {
                                if (!object.ReferenceEquals(null, g))
                                {
                                    //Payton:v4.04 added logic to account for reference sources not having a zip but matchscore indicates good match
                                    if ((g.MatchedFeatureAddress.ZIP == g.InputAddress.ZIP && g.MatchScore > score) || g.MatchedFeatureAddress.ZIP == "" && g.MatchScore > 90)
                                    //if (g.MatchedFeatureAddress.ZIP == g.InputAddress.ZIP && g.MatchScore > score)
                                    {
                                        ret.RemoveAt(i);
                                        ret.Insert(0, g);
                                        break;
                                    }
                                }
                                i++;
                            }
                        }
                        catch (Exception e)
                        {
                            throw new Exception("BOO in checkForBetterMatch " + e.InnerException + " and msg: " + e.Message + "and record is: " + Convert.ToString(i) + "and value1 is: " + geocodes[i - 1].ToString() + "and value2 is: " + geocodes[i].ToString() + "and value2 is: " + geocodes[i + 1].ToString());
                        }
                    }
                    else //if first address zip is correct then there is no need to test remaining geocodes for better match
                    {
                        ret = geocodes;
                    }
                }
                //if no valid geocodes exist ret needs to add top to be unmatchable
                else
                {
                    ret.Add(GeocodeCollection.Geocodes[0]);
                }
                //GeocodeCollection.Geocodes = ret;
            }
            //GeocodeCollection.Geocodes = ret;
            return ret;
        }

        public List<IGeocode> SortByConfidence(List<IGeocode> geocodes)
        {
            List<IGeocode> ret = new List<IGeocode>();
            List<IGeocode> geocodeList = new List<IGeocode>();
            if (geocodes.Count > 0) {

                int i = 0;
                try
                {
                    foreach (IGeocode g in geocodes)
                    {
                        if (!object.ReferenceEquals(null, g))
                        {
                            if (g.Valid)
                            {
                                geocodeList.Add(g);
                            }
                        }
                        i++;
                    }
                }
                catch (Exception e)
                {
                    throw new Exception("BOO in getValidGeocodes " + e.InnerException + " and msg: " + e.Message + "and record is: " + Convert.ToString(i) + "and value1 is: " + geocodes[i - 1].ToString() + "and value2 is: " + geocodes[i].ToString() + "and value2 is: " + geocodes[i + 1].ToString());
                }

                //This is nothing but a placeholder. It's an ok sort but we need to get a better sort here                
                ret = geocodeList.OrderBy(d => d.NAACCRGISCoordinateQualityCode).ThenByDescending(d => d.MatchScore).ToList();

                //PAYTON:v4.03 Updating to return lower level street match if the zipcode matches input zipcode
                if (geocodes.Count > 0)
                {
                    if (ret[0].MatchedFeatureAddress.ZIP != ret[0].InputAddress.ZIP)
                    {
                        double score = ret[0].MatchScore;
                        i = 0;
                        try
                        {
                            foreach (IGeocode g in geocodes)
                            {
                                if (!object.ReferenceEquals(null, g))
                                {
                                    //Payton:v4.04 added logic to account for reference sources not having a zip but matchscore indicates good match
                                    if ((g.MatchedFeatureAddress.ZIP == g.InputAddress.ZIP && g.MatchScore > score) || g.MatchedFeatureAddress.ZIP == "" && g.MatchScore > 90)
                                    //if (g.MatchedFeatureAddress.ZIP == g.InputAddress.ZIP && g.MatchScore > score)
                                    {
                                        ret.RemoveAt(i);
                                        ret.Insert(0, g);
                                        break;
                                    }
                                }
                                i++;
                            }
                        }
                        catch (Exception e)
                        {
                            throw new Exception("BOO in getValidGeocodes " + e.InnerException + " and msg: " + e.Message + "and record is: " + Convert.ToString(i) + "and value1 is: " + geocodes[i - 1].ToString() + "and value2 is: " + geocodes[i].ToString() + "and value2 is: " + geocodes[i + 1].ToString());
                        }
                    }
                }
                //if no valid geocodes exist ret needs to add top to be unmatchable
                else
                {
                    ret.Add(GeocodeCollection.Geocodes[0]);
                }
            }
            if (ret.Count < 1)
            {
                ret = geocodeList;
            }
            return ret;
        }

        public List<IGeocode> SortByConfidence(List<IGeocode> geocodes, GeocoderConfiguration geocoderConfiguration)
        {
            List<IGeocode> ret = new List<IGeocode>();
            List<IGeocode> geocodeList = new List<IGeocode>();
            if (geocodes.Count > 0)
            {

                int i = 0;
                try
                {
                    foreach (IGeocode g in geocodes)
                    {
                        if (!object.ReferenceEquals(null, g))
                        {
                            if (g.Valid)
                            {
                                geocodeList.Add(g);
                            }
                        }
                        i++;
                    }
                }
                catch (Exception e)
                {
                    throw new Exception("BOO in SortByConfidence " + e.InnerException + " and msg: " + e.Message + "and record is: " + Convert.ToString(i) + "and value1 is: " + geocodes[i - 1].ToString() + "and value2 is: " + geocodes[i].ToString() + "and value2 is: " + geocodes[i + 1].ToString());
                }

                var geoRefList = geocodes.ToList(); //this is the current geocode order
                var ReferenceDataSources = geocoderConfiguration.ReferenceDatasetConfiguration.ReferenceDataSources; //this is the preferred reference order

                foreach (var reference in ReferenceDataSources)
                {

                    PropertyInfo pi = reference.GetType().GetProperty("Name");
                    string refTxt = (String)(pi.GetValue(reference, null));
                    foreach (var geoRef in geoRefList)
                    {
                        if (refTxt.Contains(geoRef.SourceType))
                        {
                            ret.Add(geoRef);
                            break;
                        }
                    }
                }
                //This is nothing but a placeholder. It's an ok sort but we need to get a better sort here
                //ret = geocodeList.OrderBy(d => d.NAACCRGISCoordinateQualityCode).ThenByDescending(d => d.MatchScore).ToList();
                //PAYTON:v4.03 Updating to return lower level street match if the zipcode matches input zipcode
                if (geocodes.Count > 0)
                {
                    if (ret[0].MatchedFeatureAddress.ZIP != ret[0].InputAddress.ZIP)
                    {
                        double score = ret[0].MatchScore;
                        i = 0;
                        try
                        {
                            foreach (IGeocode g in ret)
                            {
                                if (!object.ReferenceEquals(null, g))
                                {
                                    //Payton:v4.04 added logic to account for reference sources not having a zip but matchscore indicates good match
                                    if ((g.MatchedFeatureAddress.ZIP == g.InputAddress.ZIP && g.MatchScore > score) || g.MatchedFeatureAddress.ZIP == "" && g.MatchScore > 90)
                                    //if (g.MatchedFeatureAddress.ZIP == g.InputAddress.ZIP && g.MatchScore > score)
                                    {
                                        ret.RemoveAt(i);
                                        ret.Insert(0, g);
                                        break;
                                    }
                                }
                                i++;
                            }
                        }
                        catch (Exception e)
                        {
                            throw new Exception("BOO in checkForBetterMatch " + e.InnerException + " and msg: " + e.Message + "and record is: " + Convert.ToString(i) + "and value1 is: " + geocodes[i - 1].ToString() + "and value2 is: " + geocodes[i].ToString() + "and value2 is: " + geocodes[i + 1].ToString());
                        }
                    }
                }
                //if no valid geocodes exist ret needs to add top to be unmatchable
                else
                {
                    ret.Add(GeocodeCollection.Geocodes[0]);
                }
            }

            return ret;
        }

        //PAYTON:MICROMATCHSTATUS - we need to determine the actual micro match status here - this is just a placeholder
        public bool GetMicroMatchStatus()
        {
            bool ret = false;
            List<IGeocode> geocodesIn = GeocodeCollection.GetValidGeocodes();
            List<IGeocode> geocodes = SortByConfidence(geocodesIn);
            if (geocodes != null)            {
                
                if (geocodes.Count > 0)
                {
                    //PAYTON:PenaltyCode
                    if (geocodes[0].Version >= 4.4)
                    {
                        this.PenaltyCodeResult = new PenaltyCodeResult();
                    }
                    // Coordinate code should not be used here as a street segment should be a viable match as well as parcel, point etc
                    //if (geocodes[0].NAACCRGISCoordinateQualityCode == "00" && geocodes[0].MatchScore > 90)
                    if (geocodes[0].MatchScore < 100)
                    {
                        if (geocodes[0].MatchScore > 84)
                        {
                            if (geocodes[0].MatchedFeatureAddress.City != null && geocodes[0].MatchedFeatureAddress.ZIP != null)
                            {
                                if (geocodes[0].MatchedFeatureAddress.City.ToUpper() == geocodes[0].InputAddress.City.ToUpper() && geocodes[0].MatchedFeatureAddress.ZIP == geocodes[0].InputAddress.ZIP && geocodes[0].MatchScore > 97)
                                {
                                    this.MicroMatchStatus = "Match";
                                }
                                else
                                {
                                    this.MicroMatchStatus = "Review";
                                    //double avgDistance = getAverageDistance();
                                    parcelMatches = 0;
                                    streetMatches = 0;
                                    double avgParcelDistance = getAverageDistance("parcel");
                                    double avgStreetDistance = getAverageDistance("street");
                                    //if (avgDistance < .05 && geocodes.Count > 5 && getCensusMatchStatus())
                                    //{
                                    //    this.MicroMatchStatus = "Match";
                                    //}
                                    if (avgParcelDistance < .05 && parcelMatches > 1 && getCensusMatchStatus())
                                    {
                                        this.MicroMatchStatus = "Match";
                                    }
                                    if (parcelMatches == 0 && streetMatches > 1 && avgStreetDistance < .05 && getCensusMatchStatus())
                                    {
                                        this.MicroMatchStatus = "Match";
                                    }
                                    if (geocodes[0].Version >= 4.4)
                                    {
                                        getDistancePenalty((avgParcelDistance + avgStreetDistance) / 2);
                                    }
                                }
                            }
                            else
                            {
                                this.MicroMatchStatus = "Review";
                            }
                        }
                        else //anything not match or review is returned as non-match
                        {
                            this.MicroMatchStatus = "Non-Match";
                        }
                    }
                    else //if we reach here then matchscore is 100 and we return a "Match"
                    {
                        //PAYTON:PENALTYCODE
                        if (geocodes[0].Version >= 4.4)
                        {
                            if (geocodes[0].InputAddress.City != geocodes[0].MatchedFeatureAddress.City && CityUtils.isValidAlias(geocodes[0].InputAddress.City, geocodes[0].MatchedFeatureAddress.City, geocodes[0].InputAddress.State))
                            {
                                this.PenaltyCodeResult.city = "1";
                            }
                        }
                        this.MicroMatchStatus = "Match";
                    }
                }
                else //if no matches were found - return Non-match
                {
                    this.MicroMatchStatus = "Non-Match";
                }
                //PAYTON:PenaltyCode - only available in version 4.04 and after
                if (geocodes.Count>0 && geocodes[0].Version >= 4.4)
                {

                    Dictionary<string, string> scoreResult = new Dictionary<string, string>();
                    var matchedScoreResults = geocodes[0].MatchedFeature.MatchScoreResult.MatchScorePenaltyResults;
                    foreach (var penalty in matchedScoreResults)
                    {
                        scoreResult.Add(penalty.AddressComponent.ToString(), penalty.PenaltyValue.ToString());
                    }
                    try
                    {
                        string pre = "0";
                        string post = "0";
                        if (scoreResult.ContainsKey("PreDirectional"))
                        {
                            pre = scoreResult["PreDirectional"];
                        }
                        if (scoreResult.ContainsKey("PostDirectional"))
                        {
                            post = scoreResult["PostDirectional"];
                        }                       
                        string inputStreet = geocodes[0].InputAddress.PreDirectional + " " + geocodes[0].InputAddress.StreetName + " " + geocodes[0].InputAddress.PostDirectional;
                        string featureStreet = geocodes[0].MatchedFeatureAddress.PreDirectional + " " + geocodes[0].MatchedFeatureAddress.StreetName + " " + geocodes[0].MatchedFeatureAddress.PostDirectional;
                        if (Convert.ToDouble(pre) > 0 || Convert.ToDouble(post) > 0)
                        {
                            //this.PenaltyCodeResult.assignDirectionalPenalty(inputStreet, featureStreet);
                            this.PenaltyCodeResult.assignDirectionalPenalty(geocodes[0].InputAddress.PreDirectional, geocodes[0].MatchedFeatureAddress.PreDirectional, geocodes[0].InputAddress.PostDirectional, geocodes[0].MatchedFeatureAddress.PostDirectional);
                        }
                    }
                    catch (Exception e)
                    {
                        string msg = "error getting scoreResults " + e.Message;
                    }
                    getPenaltyCodeInputType(geocodes);
                    getPenaltyCodeStreetType(geocodes);
                    this.PenaltyCodeResult.assignStreetNamePenalty(geocodes[0].InputAddress.StreetName, geocodes[0].MatchedFeatureAddress.StreetName, geocodes[0].MatchType, geocodes[0].NAACCRGISCoordinateQualityCode);
                    this.PenaltyCodeResult.getPenalty(scoreResult);
                    this.PenaltyCode = this.PenaltyCodeResult.getPenaltyString();
                }
                else
                {
                    this.PenaltyCodeResult = new PenaltyCodeResult();
                    this.PenaltyCode = this.PenaltyCodeResult.getPenaltyString();
                }
            }
            else
            {
                this.MicroMatchStatus = "Non-Match";
                this.PenaltyCodeResult = new PenaltyCodeResult();
                this.PenaltyCode = this.PenaltyCodeResult.getPenaltyString();
            }
            this.GeocodeCollection.Geocodes = geocodes;
            return ret;
        }

        public void getPenaltyCodeInputType(List<IGeocode> geocodes)
        {
            bool hasNumber = (geocodes[0].ParsedAddress.Number != "" && geocodes[0].ParsedAddress.Number != null);
            bool hasNumberFrac = (geocodes[0].ParsedAddress.NumberFractional != "" && geocodes[0].ParsedAddress.NumberFractional != null);
            bool hasName = (geocodes[0].ParsedAddress.StreetName != "" && geocodes[0].ParsedAddress.StreetName != null);
            bool hasCity = geocodes[0].ParsedAddress.HasCity;
            bool hasZip = (geocodes[0].ParsedAddress.ZIP != "");


            if (!hasNumber && !hasNumberFrac && !hasName)
            {
                if (hasCity)
                {
                    if (!hasZip)
                    {
                        this.PenaltyCodeResult.inputType = "3";
                    }
                    else
                    {
                        this.PenaltyCodeResult.inputType = "4";
                    }
                }
                else
                {
                    this.PenaltyCodeResult.inputType = "5";
                }
            }
            else if (!hasNumber && !hasNumberFrac && hasName)
            {
                this.PenaltyCodeResult.inputType = "1";
            }
            else if ((hasNumber || hasNumberFrac) && !hasName)
            {
                this.PenaltyCodeResult.inputType = "2";
            }
        }

        public void getPenaltyCodeStreetType(List<IGeocode> geocodes)
        {
            if (geocodes[0].ParsedAddress.HasPostOfficeBoxNumber || geocodes[0].ParsedAddress.HasPostOfficeBox)
            {
                this.PenaltyCodeResult.streetType = "1";
            }
            else if (geocodes[0].ParsedAddress.HasRuralRoute || geocodes[0].ParsedAddress.HasRuralRouteBox || geocodes[0].ParsedAddress.HasRuralRouteBoxNumber || geocodes[0].ParsedAddress.HasRuralRouteNumber)
            {
                this.PenaltyCodeResult.streetType = "2";
            }
            else if (geocodes[0].ParsedAddress.HasHighwayContractRoute || geocodes[0].ParsedAddress.HasHighwayContractRouteBox || geocodes[0].ParsedAddress.HasHighwayContractRouteBoxNumber || geocodes[0].ParsedAddress.HasHighwayContractRouteNumber)
            {
                this.PenaltyCodeResult.streetType = "3";
            }
            else if (geocodes[0].ParsedAddress.HasStarRoute || geocodes[0].ParsedAddress.HasStarRouteBox || geocodes[0].ParsedAddress.HasStarRouteBoxNumber || geocodes[0].ParsedAddress.HasStarRouteNumber)
            {
                this.PenaltyCodeResult.streetType = "4";
            }
        }

    public double getAverageDistance(string type)
        {
            List<IGeocode> geocodesIn = GeocodeCollection.GetValidGeocodes();
            List<IGeocode> geocodes = SortByConfidence(geocodesIn);
            int num_points = 0;           
            //List<Point> normalPoints = new List<Point>();
            List<PointF> points = new List<PointF>();
            foreach (var resultPoint in geocodes)
            {
                //normalPoints.Add(new Point(Convert.ToInt32(resultPoint.Longitude), Convert.ToInt32(resultPoint.Latitude)));
                if (type == "parcel")
                {
                    if (resultPoint.NAACCRGISCoordinateQualityType == NAACCRGISCoordinateQualityType.AddressPoint ||
                       resultPoint.NAACCRGISCoordinateQualityType == NAACCRGISCoordinateQualityType.Parcel)
                    {
                        points.Add(new PointF(Convert.ToSingle(resultPoint.Longitude), Convert.ToSingle(resultPoint.Latitude)));
                        parcelMatches++;
                        num_points++;
                    }
                }
                else if (type == "street")
                {
                    if (resultPoint.NAACCRGISCoordinateQualityType == Metadata.Qualities.NAACCRGISCoordinateQualityType.StreetCentroid ||
                        resultPoint.NAACCRGISCoordinateQualityType == Metadata.Qualities.NAACCRGISCoordinateQualityType.StreetIntersection ||
                        resultPoint.NAACCRGISCoordinateQualityType == Metadata.Qualities.NAACCRGISCoordinateQualityType.StreetSegmentInterpolation)
                    {
                        points.Add(new PointF(Convert.ToSingle(resultPoint.Longitude), Convert.ToSingle(resultPoint.Latitude)));
                        streetMatches++;
                        num_points++;
                    }
                }
            }
            PointF[] pts = new PointF[num_points];
            points.CopyTo(pts, 0);
            //pts[num_points] = points[0];
            float area = 0;
            double distance = 0;
            double distanceAvg = 0;
            if (points.Count > 1)
            {
                for (int i = 0; i < num_points - 1; i++)
                {
                    //area +=
                    //    (pts[i + 1].X - pts[i].X) *
                    //    (pts[i + 1].Y + pts[i].Y) / 2;
                    double dX = pts[0].X - pts[i + 1].X;
                    double dY = pts[0].Y - pts[i + 1].Y;
                    double multi = dX * dX + dY * dY;
                    distance = distance + Math.Round(Math.Sqrt(multi), 3);
                }
                distanceAvg = ((distance) / (num_points - 1)) * 100;
            }
            else
            {
                distanceAvg = 0;
            }
            return distanceAvg;
        }

        public bool getCensusMatchStatus()
        {
            bool censusMatches = true;
            List<IGeocode> geocodesIn = GeocodeCollection.GetValidGeocodes();
            List<IGeocode> geocodes = SortByConfidence(geocodesIn);
            if (geocodes[0].CensusRecords != null)
            {
                if (geocodes[0].CensusRecords.Count > 0)
                {
                    string censusTract = geocodes[0].CensusRecords[0].CensusTract.ToString();
                    string censusBlock = geocodes[0].CensusRecords[0].CensusBlock.ToString();
                    string countyFips = geocodes[0].CensusRecords[0].CensusCountyFips.ToString();
                    foreach (var geocode in geocodes)
                    {
                        if (geocode.CensusRecords[0].CensusBlock != censusBlock)
                        {
                            censusMatches = false;
                            break;
                        }
                        else if (geocode.CensusRecords[0].CensusTract != censusTract)
                        {
                            censusMatches = false;
                            break;
                        }
                        else if (geocode.CensusRecords[0].CensusCountyFips != countyFips)
                        {
                            censusMatches = false;
                            break;
                        }
                    }
                }
                else
                {
                    censusMatches = false;
                }
            }
            else
            {
                censusMatches = false;
            }
            return censusMatches;
        }
        public double getAverageDistance()
        {
            List<IGeocode> geocodesIn = GeocodeCollection.GetValidGeocodes();
            List<IGeocode> geocodes = SortByConfidence(geocodesIn);
            int num_points = geocodes.Count;
            PointF[] pts = new PointF[num_points + 1];
            //List<Point> normalPoints = new List<Point>();
            List<PointF> points = new List<PointF>();
            foreach (var resultPoint in geocodes)
            {
                //normalPoints.Add(new Point(Convert.ToInt32(resultPoint.Longitude), Convert.ToInt32(resultPoint.Latitude)));
                points.Add(new PointF(Convert.ToSingle(resultPoint.Longitude), Convert.ToSingle(resultPoint.Latitude)));
            }
            points.CopyTo(pts, 0);
            pts[num_points] = points[0];
            float area = 0;
            double distance = 0;
            for (int i = 0; i < num_points; i++)
            {
                //area +=
                //    (pts[i + 1].X - pts[i].X) *
                //    (pts[i + 1].Y + pts[i].Y) / 2;
                double dX = pts[0].X - pts[i + 1].X;
                double dY = pts[0].Y - pts[i + 1].Y;
                double multi = dX * dX + dY * dY;
                distance = distance + Math.Round(Math.Sqrt(multi), 3);
            }
            double distanceAvg = ((distance) / num_points) * 100;
            return distanceAvg;
        }
        public bool GetMicroMatchStatus(GeocoderConfiguration geocoderConfiguration)
        {
            bool ret = false;
            //            

            List<IGeocode> geocodesIn = GeocodeCollection.GetValidGeocodes();
            List<IGeocode> geocodes = SortByConfidence(geocodesIn, geocoderConfiguration);
            if (geocodes.Count > 0)
            {
                // Coordinate code should not be used here as a street segment should be a viable match as well as parcel, point etc
                //if (geocodes[0].NAACCRGISCoordinateQualityCode == "00" && geocodes[0].MatchScore > 90)
                if (geocodes[0].MatchScore < 100)
                {
                    if (geocodes[0].MatchScore > 84)
                    {
                        if (geocodes[0].MatchedFeatureAddress.City != null && geocodes[0].MatchedFeatureAddress.ZIP != null)
                        {
                            if (geocodes[0].MatchedFeatureAddress.City.ToUpper() == geocodes[0].InputAddress.City.ToUpper() && geocodes[0].MatchedFeatureAddress.ZIP == geocodes[0].InputAddress.ZIP && geocodes[0].MatchScore > 97)
                            {
                                this.MicroMatchStatus = "Match";
                            }
                            else
                            {
                                this.MicroMatchStatus = "Review";
                                double avgDistance = getAverageDistance();
                                //If the average distance is less than 1/5 of a mile - assume it's a good match
                                //Adding a count check as well to account for all navteq references to return a non-valid match but all the same coords
                                //if count is > 5 it's safe to assume that multiple references are reporting the same location for the address
                                if (avgDistance < .05 && geocodes.Count > 5)
                                {
                                    this.MicroMatchStatus = "Match";
                                }
                                getDistancePenalty(avgDistance);
                            }
                        }
                        else
                        {
                            this.MicroMatchStatus = "Review";
                        }
                    }
                    else //anything not match or review is returned as non-match
                    {
                        this.MicroMatchStatus = "Non-Match";
                    }
                }
                else //if we reach here then matchscore is 100 and we return a "Match"
                {
                    //PAYTON:PENALTYCODE
                    if (geocodes[0].InputAddress.City != geocodes[0].MatchedFeatureAddress.City && CityUtils.isValidAlias(geocodes[0].InputAddress.City, geocodes[0].MatchedFeatureAddress.City, geocodes[0].InputAddress.State))
                    {
                        this.PenaltyCodeResult.city = "1";
                    }
                    this.MicroMatchStatus = "Match";
                }
            }
            else //anything not match or review is returned as non-match
            {
                this.MicroMatchStatus = "Non-Match";
            }
            return ret;
        }

        public void getDistancePenalty(double avgDistance)
        {
            if (avgDistance <= .00094697 && avgDistance > 0) //5ft or less
            {
                this.PenaltyCodeResult.distance = "-";
            }
            else if (avgDistance <= 0.00473485 && avgDistance > .00094697) //+5ft-25ft
            {
                this.PenaltyCodeResult.distance = "1";
            }
            else if (avgDistance <= 0.0094697 && avgDistance > 0.00473485) //+25ft-50ft
            {
                this.PenaltyCodeResult.distance = "2";
            }
            else if (avgDistance <= 0.0189394 && avgDistance > 0.0094697) //+50ft-100ft
            {
                this.PenaltyCodeResult.distance = "3";
            }
            else if (avgDistance <= 0.0473485 && avgDistance > 0.0189394) //+100ft-250ft
            {
                this.PenaltyCodeResult.distance = "4";
            }
            else if (avgDistance <= 0.094697 && avgDistance > 0.0473485) //+250ft-500ft
            {
                this.PenaltyCodeResult.distance = "5";
            }
            else if (avgDistance > 0.094697)  //+500ft
            {
                this.PenaltyCodeResult.distance = "6";
            }
        }
    }
}
