module SourceData

open Fable.SimpleHttp
open Fable.SimpleJson

open Types

let private statsDataUrl, regionsDataUrl =
    ("https://covid19.rthand.com/api/stats",
     "https://covid19.rthand.com/api/regions")

type TransferAgeGroup =
    { ageFrom : int option
      ageTo : int option
      allToDate : int option
      femaleToDate : int option
      maleToDate : int option }

    member this.ToDomain : AgeGroup =
        { AgeFrom = this.ageFrom
          AgeTo = this.ageTo
          TestedPositiveMale = this.maleToDate
          TestedPositiveFemale = this.femaleToDate
          TestedPositiveAll = this.allToDate }

type private TransferStatsDataPoint =
    { dayFromStart : int
      year : int
      month : int
      day : int
      phase : string
      performedTestsToDate : int option
      performedTests : int option
      positiveTestsToDate : int option
      positiveTests : int option
      cases :
        {| confirmedToday : int option
           confirmedToDate : int option
           closedToDate : int option
           activeToDate : int option |}
      statePerTreatment :
        {| inHospital : int option
           inHospitalToDate : int option
           inICU : int option
           critical : int option
           deceasedToDate : int option
           deceased : int option
           outOfHospitalToDate : int option
           outOfHospital : int option
           recoveredToDate : int option |}
      statePerAgeToDate : TransferAgeGroup list
    }

    member this.ToDomain : StatsDataPoint =
        { DayFromStart = this.dayFromStart
          Date = System.DateTime(this.year, this.month, this.day)
          Phase = this.phase
          PerformedTests = this.performedTests
          PerformedTestsToDate = this.performedTestsToDate
          PositiveTests = this.positiveTests
          PositiveTestsToDate = this.positiveTestsToDate
          Cases =
            { ConfirmedToday = this.cases.confirmedToday
              ConfirmedToDate = this.cases.confirmedToDate
              ClosedToDate = this.cases.closedToDate
              ActiveToDate = this.cases.activeToDate }
          StatePerTreatment =
            { InHospital = this.statePerTreatment.inHospital
              InHospitalToDate = this.statePerTreatment.inHospitalToDate
              InICU = this.statePerTreatment.inICU
              Critical = this.statePerTreatment.critical
              DeceasedToDate = this.statePerTreatment.deceasedToDate
              Deceased = this.statePerTreatment.deceased
              OutOfHospitalToDate = this.statePerTreatment.outOfHospitalToDate
              OutOfHospital = this.statePerTreatment.outOfHospital
              RecoveredToDate = this.statePerTreatment.recoveredToDate }
          StatePerAgeToDate = this.statePerAgeToDate |> List.map (fun item -> item.ToDomain)
        }

type private TransferStatsData = TransferStatsDataPoint list

let parseStatsData responseData =
    let transferStatsData =
        responseData
        |> SimpleJson.parse
        |> Json.convertFromJsonAs<TransferStatsData>

    transferStatsData
    |> List.map (fun transferDataPoint -> transferDataPoint.ToDomain)

let parseRegionsData data =
    data
    |> SimpleJson.parse
    |> function
        | JArray regions ->
            regions
            |> List.map (fun dataPoint ->
                match dataPoint with
                | JObject dict ->
                    let value key = Map.tryFind key dict
                    [value "year" ; value "month" ; value "day" ; value "regions" ]
                    |> List.choose id
                    |> function
                        | [JNumber year ; JNumber month ; JNumber day ; JObject regionMap] ->
                            let date = System.DateTime(int year, int month, int day)
                            let regions =
                                regionMap
                                |> Map.toList
                                |> List.map (fun (regionKey, regionValue) ->
                                    let municipalities =
                                        match regionValue with
                                        | JObject cityMap ->
                                            cityMap
                                            |> Map.toList
                                            |> List.map (fun (cityKey, cityValue) ->
                                                match cityValue with
                                                | JNumber num -> { Name = cityKey ; PositiveTests = Some (int num) }
                                                | JNull -> { Name = cityKey ; PositiveTests = None }
                                                | _ -> failwith (sprintf "nepričakovan format podatkov za mesto %s" cityKey)
                                            )
                                        | _ -> failwith (sprintf "nepričakovan format podatkov za regijo %s" regionKey)
                                    { Name = regionKey
                                      Municipalities = municipalities }
                                )
                            { Date = date
                              Regions = regions }
                        | _ -> failwith "nepričakovan format regijskih podatkov"
                | _ -> failwith "nepričakovan format regijskih podatkov"
            )
        | _ -> failwith "nepričakovan format regijskih podatkov"

let loadData =
    async {
        let! (statsStatusCode, statsResponse) = Http.get statsDataUrl
        let! (regionsStatusCode, regionsResponse) = Http.get regionsDataUrl

        if statsStatusCode <> 200 then
            return DataLoaded (sprintf "Napaka pri nalaganju statističnih podatkov: %d" statsStatusCode |> Failure)
        else if regionsStatusCode <> 200 then
            return DataLoaded (sprintf "Napaka pri nalaganju regijskih podatkov: %d" regionsStatusCode |> Failure)
        else
            try
                let data =
                  { StatsData = parseStatsData statsResponse
                    RegionsData = parseRegionsData regionsResponse }

                return DataLoaded (Success data)
            with
                | ex -> return DataLoaded (sprintf "Napaka pri branju podatkov: %s" ex.Message |> Failure)
    }