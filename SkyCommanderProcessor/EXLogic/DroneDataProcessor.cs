﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using System.Configuration;
using SkyCommanderProcessor.Models;

namespace SkyCommanderProcessor.EXLogic {

  public class SummaryGenerator {
    private const Double ResetValue = -99999D;
    private Double TotalCount = 0;
    private Double TotalValue = 0;
    private Double MinValue = ResetValue;
    private Double MaxValue = ResetValue;
    public void AddTo(Double Value) {
      if (Value == 0)
        return;

      if (MinValue == ResetValue || Value < MinValue)
        MinValue = Value;

      if (MaxValue == ResetValue || Value > MaxValue)
        MaxValue = Value;

      TotalCount++;
      TotalValue += Value;
    }

    public void Reset(Double TotalValue, Double TotalCount, Double MinValue, Double MaxValue) {
      this.TotalCount = TotalCount;
      this.TotalValue = TotalValue;
      this.MinValue = MinValue;
      this.MaxValue = MaxValue;
    }

    public Double Sum {
      get { return TotalValue; }
    }
    public Double Count {
      get { return TotalCount; }
    }
    public Double Min {
      get { return MinValue; }
    }
    public Double Max {
      get { return MaxValue; }
    }
    public Double Avg {
      get { 
      if (TotalCount == 0)
        return 0;
      return TotalValue / TotalCount;
      }
    }
  }

  public class DroneDataProcessor {
    public delegate void OnTaskErrorHandler(TaskExceptionInfo e);
    public event OnTaskErrorHandler OnTaskError;
    public delegate void DbEntityException(System.Data.Entity.Validation.DbEntityValidationException e);
    public event DbEntityException OnDbEntityException;

    //private static FlightProcessorConnection cn = new FlightProcessorConnection();
    private DroneData _DroneData;
    private DroneFlight _DroneFlight = new DroneFlight();
    private ExtDroneFight _ExtDroneFlight;
    private FlightMapData _FlightMapData = null;
    private FlightMapData _LastFlightMapData = null;
    private List<Alert> Alerts = new List<Alert>();
    private List<FlightApproval> FlightApprovals = new List<FlightApproval>();
    private FlightApproval _Approval = null;

    private SummaryGenerator AltitudeSummary = new SummaryGenerator();
    private SummaryGenerator SatelliteSummary = new SummaryGenerator();
    private SummaryGenerator SpeedSummary = new SummaryGenerator();

    private DateTime LastUpdatedOn = DateTime.Now;
    private Decimal _BBFlightID = 0;
    private int _DroneID = 0;
    private DateTime _ReadTime = System.DateTime.UtcNow;
    private int _FlightID { get; set; } = 0;
    private Decimal _TotalDistance = 0;
    private Decimal _TotalFlightTime = 0;

    private bool IsAlertedBoundary = false;
    private bool IsAlertedSoftBoundary = false;
    private bool IsAlertedAltitudeLimit = false;
    private bool IsAlertedProximityWarning = false;
    private bool IsAlertedProximityCritical = false;

    private Dictionary<int, Proximity> Proximities = new Dictionary<int, Proximity>();


    private static readonly object _LockDroneUpdate = new object();
    private readonly object _LockFlightUpdate = new object();
    private readonly object _lockProximity = new object();
    private readonly Decimal _MinFieldValue = 0.75m;

    public Double Altitude {
      get {
        if (_FlightMapData == null || _FlightMapData.Altitude == null)
          return 0;
        return (Double)_FlightMapData.Altitude;
      }
    }

    public int PilotID {
      get {
        if (_DroneFlight == null || _DroneFlight.PilotID == null)
          return 0;
        return (int)_DroneFlight.PilotID;
      }
    }

    public String PilotName {
      get {
        return _ExtDroneFlight == null ?
          String.Empty :
          _ExtDroneFlight.PilotName;
      }
    }

    public Double GetElapsed() {
      return DateTime.Now.Subtract(LastUpdatedOn).TotalSeconds;
    }

    public async Task<bool> UpdateFlightSummary() {
      Exponent.WeatherAPI weather = new Exponent.WeatherAPI();
      var CurrentCondition = weather.GetByLocation((Double)_FlightMapData.Latitude, (Double)_FlightMapData.Longitude);

      var flightInfo = new Models.FlightInfo {
        FlightID = _FlightID,
        Condition = CurrentCondition.Today.ConditionText,
        WindSpeed = CurrentCondition.Today.WindSpeed.ToString(),
        Humidity = CurrentCondition.Today.Humidity.ToString(),
        Visibility = (Decimal)CurrentCondition.Today.Visibility,
        Pressure = (Decimal)CurrentCondition.Today.Pressure,
        Temperature = CurrentCondition.Today.Temperature.ToString(),
        Wind = CurrentCondition.Today.WindDirection.ToString(),
        MaxAltiude = (Decimal)AltitudeSummary.Max,
        MinAltitude = (Decimal)AltitudeSummary.Min,
        AvgAltitude = (Decimal)AltitudeSummary.Avg,
        MaxSatellites = (Decimal)SatelliteSummary.Max,
        MinSatellites = (Decimal)SatelliteSummary.Min,
        AvgSatellites = (Decimal)SatelliteSummary.Avg,
        MaxSpeed = (Decimal)SpeedSummary.Max,
        MinSpeed = (Decimal)SpeedSummary.Min,
        AvgSpeed = (Decimal)SpeedSummary.Avg
      };
      using (FlightProcessorConnection cn = new Models.FlightProcessorConnection()) {
        cn.FlightInfo.Add(flightInfo);
        await cn.SaveChangesAsync();
      }

      return true;
    }

    public async Task<bool> GenerateReport() {
      String ReportPath = ConfigurationManager.AppSettings["ReportPath"];
      String ReportURL = ConfigurationManager.AppSettings["ReportURL"] + _FlightID.ToString();
      String ReportEmailURL = ConfigurationManager.AppSettings["ReportEmailURL"] + _FlightID.ToString();
      String PDFPath = System.IO.Path.Combine(ReportPath, $"{_FlightID}.pdf");
      
      //Generate PDF
      SelectPdf.HtmlToPdf converter = new SelectPdf.HtmlToPdf();
      converter.Options.MarginBottom = 10;
      converter.Options.MarginLeft = 10;
      converter.Options.MarginRight = 10;
      converter.Options.MarginTop = 10;
      converter.Options.EmbedFonts = true;
      converter.Options.DrawBackground = true;
      converter.Options.JavaScriptEnabled = true;
      converter.Options.KeepImagesTogether = true;
      try { 
        SelectPdf.PdfDocument doc = converter.ConvertUrl(ReportURL);
        doc.Save(PDFPath);
        doc.Close();
      } catch {
        return false;   
      }

      String NotificationEmails = _Approval?.EmailAddress;

      PortalAlertEmail portalAlertEmail = new PortalAlertEmail {
        CreatedOn = DateTime.UtcNow,
        Attachments = PDFPath,
        Body = null,
        EmailSubject = $"Post Flight Report - {_FlightID}",
        EmailURL = ReportEmailURL,
        SendType = "Email",
        ToAddress = NotificationEmails,
        IsSend = 0,
        SendStatus = "Waiting",
        UserID = PilotID,
        SendOn = null
      };

      await AlertQueue.AddToEmailQueue(portalAlertEmail);

      return false;
    }

    public GPSPoint GetPosition() {
      return new GPSPoint((float)_FlightMapData.Latitude, (float)_FlightMapData.Longitude);
    }
    public int GetFlightID() {
      return _DroneFlight.ID;
    }

    public Object GetProperty(String PropertyName) {
      switch (PropertyName.ToLower()) {
      case "dronename":
        return _ExtDroneFlight.DroneName;
      case "accountname":
        return _ExtDroneFlight.AccountName;
      case "accountid":
        return _ExtDroneFlight.AccountID;
      }
      return null;
    }

    public async Task<bool> UpdateFlightsToDB() {
      try {
        _DroneFlight.MaxSpeed = (Decimal)SpeedSummary.Max;
        _DroneFlight.MaxAltitude = (Decimal)AltitudeSummary.Max;
        _DroneFlight.FlightHours = (int)_FlightMapData.TotalFlightTime;
        _DroneFlight.FlightDistance = (Decimal)Math.Round((Double)_FlightMapData.Distance, 0);

        using (var update = new FlightProcessorConnection()) {
          var entry = update.Entry(_DroneFlight);
          entry.State = EntityState.Modified;
          await update.SaveChangesAsync();
        }
        return true;
      } catch (System.Data.Entity.Validation.DbEntityValidationException e) {
        OnDbEntityException?.Invoke(e);
      } catch (Exception ex) {
        OnTaskError?.Invoke(new TaskExceptionInfo {TaskException = ex, TaskName = "UpdateFlightsToDB()"});
      }
      return false;
    }

    public SourceGrid.Cells.Cell Cell(int CellNumber) {
      /*
       0: BB ID
       1: Flight
       2: Received Date
       3: Speed
       4: Altitude
       5: Satellite
       6: Total Time
       7: Message
       */
      switch (CellNumber) {
      case 0:
        return new SourceGrid.Cells.Cell(_DroneFlight.BBFlightID);
      case 1:
        return new SourceGrid.Cells.Cell(_DroneFlight.ID);
      case 2:
        return new SourceGrid.Cells.Cell(_ReadTime.ToString("yyyy-MM-dd HH:mm:ss"));
      case 3:
        return new SourceGrid.Cells.Cell(_FlightMapData.Speed);
      case 4:
        return new SourceGrid.Cells.Cell(_FlightMapData.Altitude);
      case 5:
        return new SourceGrid.Cells.Cell(_FlightMapData.Satellites);
      case 6:
        return new SourceGrid.Cells.Cell(_TotalFlightTime);
      case 7:
        return new SourceGrid.Cells.Cell(_TotalDistance.ToString("0.00"));
      case 8:
        return new SourceGrid.Cells.Cell(LastUpdatedOn.ToString("yyyy-MM-dd HH:mm:ss"));
      case 9:
        return new SourceGrid.Cells.Cell(GetElapsed().ToString("0.00"));
      }
      return new SourceGrid.Cells.Cell($"{CellNumber} - N/A");
    }

    public void SetDroneData(DroneData ActiveDroneData) {
      _DroneData = ActiveDroneData;

      Decimal.TryParse(_DroneData.BBFlightID, out _BBFlightID);
      _DroneID = (int)_DroneData.DroneId;
      try { 
        _ReadTime = DateTime.ParseExact(_DroneData.ReadTime, "dd/MM/yyyy-HH:mm:ss", null);
        _DroneFlight.FlightDate = _ReadTime;
      } catch(Exception ex) {
        OnTaskError?.Invoke(new TaskExceptionInfo {
          TaskException = ex,
          TaskName = "SetDroneData - _ReadTime Parse Error"
        });
      }
      _SetFlightMapData();
    }


    public  bool ProcessDroneData() {
      GPSPoint ThisPosition = new GPSPoint((float)_FlightMapData.Latitude, (float)_FlightMapData.Longitude);
      LastUpdatedOn = DateTime.Now;

      if (_LastFlightMapData != null) {
        //Fix sudden change in heading
        Double ChangeInHeading = (Double)_LastFlightMapData.Heading - (Double)_FlightMapData.Heading;
        if (ChangeInHeading > 20 || ChangeInHeading < -20) {
          if (_LastFlightMapData.Heading > 340 && _FlightMapData.Heading < 10) {
            //then we leave it to rotate
          } else {
            _FlightMapData.Heading = _LastFlightMapData.Heading + (ChangeInHeading > 0 ? 1 : -1) * 20;
            if (_FlightMapData.Heading > 360)
              _FlightMapData.Heading = _FlightMapData.Heading - 360;
            if (_FlightMapData.Heading < 0)
              _FlightMapData.Heading = -1 * _FlightMapData.Heading;
          }
        }

        if(_FlightMapData.Altitude >= _MinFieldValue || _FlightMapData.Speed >= _MinFieldValue) { 
          DateTime StartTime = (DateTime)(_LastFlightMapData.ReadTime);
          DateTime EndTime = (DateTime)(_FlightMapData.ReadTime);
          _TotalFlightTime += (Decimal)Math.Abs(EndTime.Subtract(StartTime).TotalSeconds);

          //Distance between Last point and this point
          GPSPoint LastLocation = new GPSPoint((float)_LastFlightMapData.Latitude, (float)_LastFlightMapData.Longitude);
          _FlightMapData.PointDistance = (Decimal)GPSCalc.GetDistance(ThisPosition, LastLocation);
          _TotalDistance += (Decimal)_FlightMapData.PointDistance;
        }


      }

      SatelliteSummary.AddTo((Double)_FlightMapData.Satellites);
      SpeedSummary.AddTo((Double)_FlightMapData.Speed);
      AltitudeSummary.AddTo((Double)_FlightMapData.Altitude);

      _FlightMapData.TotalFlightTime = _TotalFlightTime;
      _FlightMapData.Distance = _TotalDistance;

      _FlightMapData.avg_Altitude = (Decimal)AltitudeSummary.Avg;
      _FlightMapData.Max_Altitude = (Decimal)AltitudeSummary.Max;
      _FlightMapData.Min_Altitude = (Decimal)AltitudeSummary.Min;
      _FlightMapData.Avg_Satellites = (Decimal)SatelliteSummary.Avg;
      _FlightMapData.Max_Satellites = (Decimal)SatelliteSummary.Max;
      _FlightMapData.Min_Satellites = (Decimal)SatelliteSummary.Min;
      _FlightMapData.Avg_Speed = (Decimal)SpeedSummary.Avg;
      _FlightMapData.Max_Speed = (Decimal)SpeedSummary.Max;
      _FlightMapData.Min_Speed = (Decimal)SpeedSummary.Min;


      _DroneFlight.Longitude = _FlightMapData.Longitude;
      _DroneFlight.Latitude = _FlightMapData.Latitude;
      _ExtDroneFlight.Altitude = (Double)_FlightMapData.Altitude;
      _ExtDroneFlight.LastFlightDate = _ReadTime;

      //Save the last point for the reference
      _LastFlightMapData = Cloner<FlightMapData>.Clone(_FlightMapData);
      
      return true;
    }

    public async Task<bool> UpdateFlight() {
      if(Proximities != null) _FlightMapData.OtherFlightIDs = Newtonsoft.Json.JsonConvert.SerializeObject(GetSimpleProximity());
      try {
        using (FlightProcessorConnection cn = new FlightProcessorConnection()) {
          cn.FlightMapData.Add(_FlightMapData);
          cn.Entry(_DroneFlight).State = EntityState.Modified;
          await cn.SaveChangesAsync();
        }
        return true;
      } catch (System.Data.Entity.Validation.DbEntityValidationException e) {
        OnDbEntityException?.Invoke(e);
      } catch(Exception ex) {
        OnTaskError?.Invoke(new TaskExceptionInfo {
          TaskException = ex,
          TaskName = "Task - UpdateFlight()"
        });
      }

      return false;
    }

    private Dictionary<int, SimpleProximity> GetSimpleProximity() {
      Dictionary<int, SimpleProximity> simpleProximity = new Dictionary<int, SimpleProximity>();
      foreach(KeyValuePair<int, Proximity> item in Proximities) {
        simpleProximity.Add(item.Key, new SimpleProximity {
          Lat = item.Value.Lat,
          Lng = item.Value.Lng,
          Distance = item.Value.Distance
        });
      }
      return simpleProximity;
    }

    public async Task<bool> Update_MSTR_Drone() {
      try {
        using (FlightProcessorConnection cn = new FlightProcessorConnection()) {
          MSTR_Drone mSTR_Drone = await cn.MSTR_Drone.Where(w => w.DroneId == (int)_DroneFlight.DroneID).FirstOrDefaultAsync();
          if (mSTR_Drone != null) {
            mSTR_Drone.LastFlightID = _DroneFlight.ID;
            mSTR_Drone.FlightTime = _DroneFlight.FlightDate;
            cn.Entry(mSTR_Drone).State = EntityState.Modified;
            await cn.SaveChangesAsync();
          }
        }
        return true;
      } catch(System.Data.Entity.Validation.DbEntityValidationException e) {
        OnDbEntityException?.Invoke(e);
      } catch (Exception ex) {
        OnTaskError?.Invoke(new TaskExceptionInfo {
          TaskException = ex,
          TaskName = "Task - Update_MSTR_Drone()"
        });
      }
      return false;
    }

    public async Task<bool> GenerateAlerts() {
      bool _InBoundary = true;
      bool _InSoftBoundary = true;
      bool _IsInAltitudeLimit = true;

      GPSPoint ThisPosition = new GPSPoint((float)_FlightMapData.Latitude, (float)_FlightMapData.Longitude);
      FlightApproval Approval = GetApproval(ThisPosition);
      
      if (Approval != null) {
        _Approval = Approval;
        _DroneFlight.ApprovalID = _Approval.ApprovalId;

        _InBoundary = _Approval.IsInBoundary(ThisPosition);
        _InSoftBoundary = Approval.IsInSoftBoundary(ThisPosition);
        _IsInAltitudeLimit = Approval.IsInAltitudeLimit((Decimal)_FlightMapData.Altitude);
      } else {
        _Approval = GetClosesetApproval(ThisPosition);
        _InBoundary = false;
        _InSoftBoundary = false;
        //If approval found on close by, take that
        //if not, maximum to 40 meter
        _IsInAltitudeLimit = _Approval != null ?
          _Approval.IsInAltitudeLimit((Decimal)_FlightMapData.Altitude) :
          (_FlightMapData.Altitude <= 40M);
      }

      if (!_InSoftBoundary && !IsAlertedSoftBoundary) {
        //Outside the Soft Boundary, Generate alert
        var ThisAlert = new Alert(_ExtDroneFlight);
        ThisAlert.Generate("Boundary", "Critical");
        await ThisAlert.Save();
        IsAlertedSoftBoundary = true;
      } else if(_InSoftBoundary) {
        IsAlertedSoftBoundary = false;
      }

      if (!_InBoundary && !IsAlertedBoundary) {
        //Outside the Boundary, generate alert
        var ThisAlert = new Alert(_ExtDroneFlight);
        ThisAlert.Generate("Boundary", "High");
        await ThisAlert.Save();
        IsAlertedBoundary = true;
      } else if(_InBoundary) {
        IsAlertedBoundary = false;
      }

      if (!_IsInAltitudeLimit && !IsAlertedAltitudeLimit) {
        //Generate Alert for Altitude breach
        var ThisAlert = new Alert(_ExtDroneFlight);
        ThisAlert.Generate("Altitude", "Critical");
        await ThisAlert.Save();
        IsAlertedAltitudeLimit = true;
      } else if(_IsInAltitudeLimit) {
        IsAlertedAltitudeLimit = false;
      }

      //Save the current status
      _DroneFlight.ApprovalID = _Approval == null ? 0 : _Approval.ApprovalId;
      _DroneFlight.IsFlightOutside = IsAlertedBoundary;
      _DroneFlight.IsFlightSoftFence = IsAlertedSoftBoundary ? 1 : 0;
      _DroneFlight.HigherLimit = IsAlertedAltitudeLimit ? 1 : 0;

      _FlightMapData.IsOutSide = !_InBoundary;
      _FlightMapData.IsSoftFence = (_InSoftBoundary ? 0 : 1);

      return true;
    }

    public void ProximityRemove(int FlightID) {
      if(Proximities.ContainsKey(FlightID)) {
        lock (_lockProximity) {
          Proximities.Remove(FlightID);
        }
      }
    }//public void ProximityRemove

    public void ProximityAdd(Proximity ThisProximity) {
      int Key = ThisProximity.FlightID;

      if (!Proximities.ContainsKey(Key)) {
        lock(_lockProximity) { 
          Proximities.Add(Key, ThisProximity);
        }
      } else {
        Proximities[Key].Distance = ThisProximity.Distance;
        Proximities[Key].Lat = ThisProximity.Lat;
        Proximities[Key].Lng = ThisProximity.Lng;
      }
    }

    public async Task<bool> GenerateProximityAlerts() {
      //bool IsProximityWarning = false;
      //bool IsProximityCritical = false;
      List<Alert> Alerts = new List<Alert>();

      lock (_lockProximity) {
        int[] Keys = Proximities.Keys.ToArray();
        foreach (int Key in Keys) {
          Proximity Item = Proximities[Key];
          bool _IsProximityWarning = false;
          bool _IsProximityCritical = false;
          Console.WriteLine($"Key: {Key}, Distance: {Item.Distance}");
          if (Item.Distance < 1000) {
            _IsProximityCritical = true;
          } else if (Item.Distance < 2000) {
            _IsProximityWarning = true;
          } 

          if (_IsProximityWarning && !Item.IsProximityWarning ) {
            //Generate Alert for the proximity
            var ThisAlert = new Alert(_ExtDroneFlight);
            ThisAlert.Generate("Proximity", "High", Item);
            Alerts.Add(ThisAlert);
            IsAlertedProximityWarning = true;
          }

          if (_IsProximityCritical && !Item.IsProximityCritical ) {
            //Generate Alert for proximity Ctitical
            var ThisAlert = new Alert(_ExtDroneFlight);
            ThisAlert.Generate("Proximity", "Critical", Item);
            Alerts.Add(ThisAlert);
            IsAlertedProximityCritical = true;
          }

          Item.IsProximityWarning = _IsProximityWarning;
          Item.IsProximityCritical = _IsProximityCritical;

        }//foreach(Proximity Item in Proximities)

      }//lock (_lockProximity) 

      foreach(Alert ThisAlert in Alerts) {
        await ThisAlert.Save();
      }

      _DroneFlight.ProximityCritical = IsAlertedProximityCritical ? 1 : 0;
      _DroneFlight.ProximityWarning = IsAlertedProximityWarning ? 1 : 0;


      return true;
    }


    public async Task<DroneFlight> GenerateFlight() {
      using (FlightProcessorConnection cn = new FlightProcessorConnection()) {
        var Query = from d in cn.DroneFlight
                    where d.BBFlightID == _BBFlightID &&
                    d.DroneID == _DroneID
                    select d;
        if (await Query.AnyAsync()) {
          _DroneFlight = await Query.FirstAsync();
          _FlightMapData.FlightID = _FlightID = _DroneFlight.ID;
          await LoadFlightDetails();
        } else {
          //Load information about the flight from 
          await SetFlightInformation();

          //Add flight information to database
          cn.DroneFlight.Add(_DroneFlight);
          await cn.SaveChangesAsync();
          _FlightMapData.FlightID = _FlightID = _DroneFlight.ID;
        }//if(await Query.AnyAsync())
        _ExtDroneFlight = new ExtDroneFight(_DroneFlight);
      }

      //Load Approvals for the Flight
      await LoadFlightApprovals();
      await LoadExtDroneFlight();
      return _DroneFlight;
    }

    private async Task LoadExtDroneFlight() {
      using(FlightProcessorConnection Info = new FlightProcessorConnection()) {
        var Query = from d in Info.MSTR_Drone
                    where d.DroneId == _DroneID
                    select new {
                      AccountID = d.AccountID,
                      DroneName = d.DroneName
                    };
        if(await Query.AnyAsync()) {
          var Data = await Query.FirstAsync();
          _ExtDroneFlight.DroneName = Data.DroneName;
          _ExtDroneFlight.AccountID = Data.AccountID == null ? 0 : (int)Data.AccountID;
          _ExtDroneFlight.AccountName = await Info.MSTR_Account
            .Where(e => e.AccountId == _ExtDroneFlight.AccountID)
            .Select(e => e.Name)
            .FirstOrDefaultAsync();
        }
        int PilotID = (int)_ExtDroneFlight.GetBase().PilotID;
        var Query2 = from d in Info.MSTR_User
                     where d.UserId == PilotID
                     select d.FirstName + " " + d.LastName;
        if(await Query2.AnyAsync()) {
          _ExtDroneFlight.PilotName = await Query2.FirstOrDefaultAsync();
        }

      }
    }//LoadExtDroneFlight


    private async Task LoadFlightDetails() {
      using (FlightProcessorConnection cn = new FlightProcessorConnection()) {
        var Query = from e in cn.FlightMapData
                    where e.FlightID == _DroneFlight.ID
                    group e by e.FlightID into gr
                    select new {
                      MinSatellites = gr.Min(g => g.Satellites),
                      MaxSatellites = gr.Max(g => g.Satellites),
                      TotalSatellites = gr.Sum(g => g.Satellites),
                      CountSatellites = gr.Count(g => g.Satellites > 0),

                      MinAltitude = gr.Min(g => g.Altitude),
                      MaxAltitude = gr.Max(g => g.Altitude),
                      TotalAltitude = gr.Sum(g => g.Altitude),
                      CountAltitude = gr.Count(g => g.Altitude > 0),

                      MinSpeed = gr.Min(g => g.Speed),
                      MaxSpeed = gr.Max(g => g.Speed),
                      TotalSpeed = gr.Sum(g => g.Speed),
                      CountSpeed = gr.Count(g => g.Speed > 0),
                    };
        if (await Query.AnyAsync()) {
          var Data = await Query.FirstAsync();
          SatelliteSummary.Reset((Double)Data.TotalSatellites, Data.CountSatellites, (Double)Data.MinSatellites, (Double)Data.MaxSatellites);
          AltitudeSummary.Reset((Double)Data.TotalAltitude, Data.CountAltitude, (Double)Data.MinAltitude, (Double)Data.MaxAltitude);
          SpeedSummary.Reset((Double)Data.TotalSpeed, Data.CountSpeed, (Double)Data.MinSpeed, (Double)Data.MaxSpeed);
        }

        var Query2 = from e in cn.FlightMapData
                     where e.FlightID == _DroneFlight.ID
                     orderby e.FlightMapDataID descending
                     select new {
                       TotalDistace = e.Distance,
                       TotalFlightTime = e.TotalFlightTime
                     };
        if (await Query2.AnyAsync()) {
          var Data = Query2.FirstOrDefault();
          _TotalDistance = (Decimal)Data.TotalDistace;
          _TotalFlightTime = (Decimal)Data.TotalFlightTime;
        }


        IsAlertedBoundary = (bool)_DroneFlight.IsFlightOutside; //is alert generated for outside boundary;
        IsAlertedSoftBoundary = (_DroneFlight.IsFlightSoftFence == 1); //is alert generated for outside soft boundary;
        IsAlertedAltitudeLimit = _DroneFlight.HigherLimit == 1; //is alert generated for heighter limit exeed;
        IsAlertedProximityWarning = _DroneFlight.IsDistanceWarning == 1; //is alert generated for proximity warnning;
        IsAlertedProximityCritical = _DroneFlight.IsDistanceCritical == 1; //is alert generated for proximity critical message;

        //Load the status of Alert Generated to set variables
      }
    }

    public async Task LoadFlightApprovals() {
      DateTime ReadDate= new DateTime(_ReadTime.Year, _ReadTime.Month, _ReadTime.Day);
      using (FlightProcessorConnection cn = new FlightProcessorConnection()) {
        var Query = from d in cn.GCA_Approval
                    where
                      d.StartDate <= ReadDate &&
                      d.EndDate >= ReadDate &&
                      d.DroneID == _DroneID
                    orderby d.StartDate descending
                    select new FlightApproval {
                      ApprovalId = d.ApprovalID,
                      ApprovalName = d.ApprovalName,
                      EmailAddress = d.NotificationEmails,
                      Boundary = d.Coordinates,
                      SoftBoundary = d.InnerBoundaryCoord,
                      MaxAltitude = (Decimal)d.MaxAltitude,
                      MinAltitude = (Decimal)d.MinAltitude
                    };
        if (await Query.AnyAsync()) {
          FlightApprovals = await Query.ToListAsync<FlightApproval>();
        }
      }

    }

    public async Task<DroneFlight> SetFlightInformation() {
      _SetDroneFlightData();
      using (FlightProcessorConnection cn = new FlightProcessorConnection()) {
        var Query = from ds in cn.MSTR_Drone_Setup
                    where ds.DroneId == _DroneID
                    select ds.PilotUserId;
        if (await Query.AnyAsync()) {
          _DroneFlight.GSCID = _DroneFlight.PilotID = await Query.FirstOrDefaultAsync();
        }
      }
      return _DroneFlight;
    }

    private FlightApproval GetApproval(GPSPoint ThisPosition) {
      foreach(FlightApproval Approval in FlightApprovals) {
        if (Approval.IsInBoundary(ThisPosition))
          return Approval;
        if(Approval.IsInSoftBoundary(ThisPosition)) {
          return Approval;
        }
      }
      return null;
    }

    private FlightApproval GetClosesetApproval(GPSPoint ThisPosition) {
      Double MinDistance = -1;
      FlightApproval flightApproval = null;
      foreach (FlightApproval Approval in FlightApprovals) {
        Double ThisDistance = Approval.GetDistanceTo(ThisPosition);
        if(MinDistance < 0 || MinDistance < ThisDistance) {
          flightApproval = Approval;
        }
      }
      return flightApproval;
    }

    private Decimal ToDecimal(String NumRef, Decimal DevidedBy = 1, bool FixNegetive = false) {
      Decimal TempDecimal = 0;
      Decimal.TryParse(NumRef, out TempDecimal);
      var ReturnVal = TempDecimal / DevidedBy;
      if (FixNegetive && ReturnVal < 0)
        ReturnVal = 0;
      return ReturnVal;
    }

    private int ToInt32(String NumRef) {
      int TempInt = 0;
      int.TryParse(NumRef, out TempInt);
      return TempInt;
    }

    private Decimal Abs(Decimal? Diff) {
      Decimal ReturnValue = (Decimal)Diff;
      if (ReturnValue < 0)
        ReturnValue = -1 * ReturnValue;
      return ReturnValue;
    }


    private void _SetDroneFlightData() {
      _DroneFlight.BBFlightID = _BBFlightID;
      _DroneFlight.GSCID = _DroneFlight.PilotID = 0;
      _DroneFlight.FlightDate = _ReadTime;
      _DroneFlight.DroneID = _DroneID;
      _DroneFlight.CreatedOn = DateTime.UtcNow;
      _DroneFlight.IsDistanceCritical = 0;
      _DroneFlight.IsDistanceWarning = 0;
      _DroneFlight.IsFlightOutside = false;
      _DroneFlight.IsFlightSoftFence = 0;
      _DroneFlight.IsInsideTimeFrame = true;
      _DroneFlight.FlightDistance = 0;
      _DroneFlight.FlightHours = 0;
      _DroneFlight.ApprovalID = 0;
      _DroneFlight.MaxSpeed = 0;
      _DroneFlight.MaxAltitude = 0;
      _DroneFlight.BoundaryCritical = 0;
      _DroneFlight.BoundaryHigh = 0;
      _DroneFlight.BoundaryWarning = 0;
      _DroneFlight.ProximityCritical = 0;
      _DroneFlight.ProximityHigh = 0;
      _DroneFlight.ProximityWarning = 0;
      _DroneFlight.HeightCritical = 0;
      _DroneFlight.HeightHigh = 0;
      _DroneFlight.HeightWarning = 0;
    }

    private void _SetFlightMapData() {

      //Create Flight Map Data records
      _FlightMapData = new FlightMapData {
        FlightID = _FlightID,
        BBFlightID = _BBFlightID.ToString(),
        CreatedTime = DateTime.UtcNow,
        DroneId = _DroneID,
        ReadTime = _ReadTime,
        ReceivedTime = _DroneData.CreatedTime,
        Distance = 0,
        PointDistance = 0,
        IsChecked = true,
        IsSoftFence = 0,
        IsOutSide = true,

        //Initilize these values, but calculate at end of flight only
        Min_Altitude = 0,
        Max_Altitude = 0,
        avg_Altitude = 0,
        Min_Satellites = 0,
        Max_Satellites = 0,
        Avg_Satellites = 0,
        Min_Speed = 0,
        Max_Speed = 0,
        Avg_Speed = 0,


        //Unused, Just initilize
        DroneRFID = String.Empty,
        GCAID = 0,
        IsActive = true,
        IsAdsbProcessed = false,
        OtherFlightIDs = String.Empty,
        ProductId = 0,
        ProductQrCode = String.Empty,
        ProductRFID = String.Empty,
        ProductRSSI = String.Empty,
        RecordType = 0,

        //Parse it from Drone Data
        Altitude = ToDecimal(_DroneData.Altitude, 100m, true),
        FixQuality = ToDecimal(_DroneData.FixQuality),
        Heading = Math.Abs(ToDecimal(_DroneData.Heading, 100m)),
        Latitude = ToDecimal(_DroneData.Latitude, 1000000m),
        Longitude = ToDecimal(_DroneData.Longitude, 1000000m),
        Roll = ToDecimal(_DroneData.Roll, 100m),
        Pitch = ToDecimal(_DroneData.Pitch, 100m),
        Satellites = ToInt32(_DroneData.Satellites),
        Speed = Math.Abs(ToDecimal(_DroneData.Speed, 100m, true)),
        voltage = ToDecimal(_DroneData.Voltage, 100m),
        TotalFlightTime = 0        

      };
      /*
      if (_FlightMapData.Altitude < _MinFieldValue)
        _FlightMapData.Altitude = 0;
      if (_FlightMapData.Speed < _MinFieldValue)
        _FlightMapData.Speed = 0;
      */
    }
  }
}
