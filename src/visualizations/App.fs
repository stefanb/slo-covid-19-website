module App

open Browser
open Elmish
open Feliz

open Types

let init (query : obj) (visualization : string option) =
    let inner () =
        let renderingMode =
            match visualization with
            | None -> Normal
            | Some viz ->
                match viz with
                | "Map" -> Some Map
                | "MetricsComparison" -> Some MetricsComparison
                | "Patients" -> Some Patients
                | "Tests" -> Some Tests
                | "Cases" -> Some Cases
                | "Spread" -> Some Spread
                | "Regions" -> Some Regions
                | "Municipalities" -> Some Municipalities
                | "AgeGroups" -> Some AgeGroups
                | "Hospitals" -> Some Hospitals
                | "Infections" -> Some Infections
                | "Countries" -> Some Countries
                | _ -> None
                |> Embedded

        let initialState =
            { Query = query
              StatsData = NotAsked
              RegionsData = NotAsked
              RenderingMode = renderingMode }

        initialState, Cmd.batch [Cmd.ofMsg StatsDataRequested ; Cmd.ofMsg RegionsDataRequest ]
    inner

let update (msg: Msg) (state: State) =
    match msg with
    | StatsDataRequested ->
        match state.StatsData with
        | Loading -> state, Cmd.none
        | _ -> { state with StatsData = Loading }, Cmd.OfAsync.result Data.Stats.load
    | StatsDataLoaded data ->
        { state with StatsData = data }, Cmd.none
    | RegionsDataRequest ->
        match state.RegionsData with
        | Loading -> state, Cmd.none
        | _ -> { state with RegionsData = Loading }, Cmd.OfAsync.result Data.Regions.load
    | RegionsDataLoaded data ->
        { state with RegionsData = data }, Cmd.none

open Elmish.React

let render (state : State) (_ : Msg -> unit) =
    let allVisualizations: Visualization list =
        [ { VisualizationType = Hospitals;
             ClassName = "hospitals-chart";
             Label = "Kapacitete bolnišnic";
             Explicit = true;
             Renderer = fun _ -> lazyView HospitalsChart.hospitalsChart () }
          { VisualizationType = MetricsComparison;
             ClassName = "metrics-comparison-chart";
             Label = "Stanje COVID-19 v Sloveniji";
             Explicit = false;
             Renderer = fun state ->
                match state.StatsData with
                | NotAsked -> Html.none
                | Loading -> Utils.renderLoading
                | Failure error -> Utils.renderErrorLoading error
                | Success data -> lazyView MetricsComparisonChart.metricsComparisonChart {| data = data |} }
          { VisualizationType = Cases;
             ClassName = "cases-chart";
             Label = "Potrjeni primeri";
             Explicit = false;
             Renderer = fun state ->
                match state.StatsData with
                | NotAsked -> Html.none
                | Loading -> Utils.renderLoading
                | Failure error -> Utils.renderErrorLoading error
                | Success data -> lazyView CasesChart.casesChart {| data = data |} }
          { VisualizationType = Patients;
             ClassName = "patients-chart";
             Label = "Hospitalizirani";
             Explicit = false;
             Renderer = fun _ -> lazyView PatientsChart.patientsChart () }
          { VisualizationType = Tests;
             ClassName = "tests-chart";
             Label = "Testiranje";
             Explicit = false;
             Renderer = fun state ->
                match state.StatsData with
                | NotAsked -> Html.none
                | Loading -> Utils.renderLoading
                | Failure error -> Utils.renderErrorLoading error
                | Success data -> lazyView TestsChart.testsChart {| data = data |} }
          { VisualizationType = Infections;
             ClassName = "infections-chart";
             Label = "Struktura potrjeno okuženih";
             Explicit = false;
             Renderer = fun state ->
               match state.StatsData with
               | NotAsked -> Html.none
               | Loading -> Utils.renderLoading
               | Failure error -> Utils.renderErrorLoading error
               | Success data -> lazyView InfectionsChart.infectionsChart {| data = data |} }
          { VisualizationType = Spread;
             ClassName = "spread-chart";
             Label = "Prirast potrjeno okuženih";
             Explicit = false;
             Renderer = fun state ->
                match state.StatsData with
                | NotAsked -> Html.none
                | Loading -> Utils.renderLoading
                | Failure error -> Utils.renderErrorLoading error
                | Success data -> lazyView SpreadChart.spreadChart {| data = data |} }
          { VisualizationType = Regions;
             ClassName = "regions-chart";
             Label = "Potrjeno okuženi po regijah";
             Explicit = false;
             Renderer = fun state ->
                match state.RegionsData with
                | NotAsked -> Html.none
                | Loading -> Utils.renderLoading
                | Failure error -> Utils.renderErrorLoading error
                | Success data -> lazyView RegionsChart.regionsChart {| data = data |} }
          { VisualizationType = Map;
             ClassName = "map-chart";
             Label = "Zemljevid potrjeno okuženih po občinah";
             Explicit = false;
             Renderer = fun state ->
                match state.RegionsData with
                | NotAsked -> Html.none
                | Loading -> Utils.renderLoading
                | Failure error -> Utils.renderErrorLoading error
                | Success data -> lazyView Map.mapChart {| data = data |} }
          { VisualizationType = Municipalities;
             ClassName = "municipalities-chart";
             Label = "Potrjeno okuženi po občinah";
             Explicit = false;
             Renderer = fun state ->
                match state.RegionsData with
                | NotAsked -> Html.none
                | Loading -> Utils.renderLoading
                | Failure error -> Utils.renderErrorLoading error
                | Success data ->
                    lazyView
                        MunicipalitiesChart.municipalitiesChart
                        {| query = state.Query ; data = data |} }
          { VisualizationType = AgeGroups;
             ClassName = "age-groups-chart";
             Label = "Po starostnih skupinah";
             Explicit = false;
             Renderer = fun state ->
                match state.StatsData with
                | NotAsked -> Html.none
                | Loading -> Utils.renderLoading
                | Failure error -> Utils.renderErrorLoading error
                | Success data ->
                    lazyView AgeGroupsChart.renderChart {| data = data |} }
          { VisualizationType = Countries;
             ClassName = "countries-chart";
             Label = "Primerjava po državah";
             Explicit = false;
             Renderer = fun state ->
               match state.StatsData with
               | NotAsked -> Html.none
               | Loading -> Utils.renderLoading
               | Failure error -> Utils.renderErrorLoading error
               | Success _ ->
                    lazyView CountriesChartViz.Rendering.renderChart ()
            }
        ]

    let embedded, visualizations =
        match state.RenderingMode with
        | Normal -> false, allVisualizations |> List.filter (fun viz -> not viz.Explicit)
        | Embedded visualizationType ->
            match visualizationType with
            | None -> true, []
            | Some visualizationType ->
                true, allVisualizations
                |> List.filter (fun viz ->
                    viz.VisualizationType = visualizationType)

    let brandLink =
        match state.RenderingMode with
        | Normal -> Html.none
        | Embedded _ ->
            Html.a
                [ prop.className "brand-link"
                  prop.target "_blank"
                  prop.href "https://covid-19.sledilnik.org/"
                  prop.text "covid-19.sledilnik.org" ]

    let renderChartTitle (visualization: Visualization) =

        let scrollToElement (e : MouseEvent) visualizationId =
            e.preventDefault()
            let element = document.getElementById(visualizationId)
            let offset = -100.
            let position = element.getBoundingClientRect().top + window.pageYOffset + offset
            window.scrollTo({| top = position ; behavior = "smooth" |} |> unbox) // behavior = smooth | auto
            window.history.pushState(null, null, "#" + visualizationId)

        Html.div [
            prop.className "title-brand-wrapper"
            prop.children
                [
                    Html.a
                        [ prop.href ("#" + visualization.ClassName)
                          prop.text visualization.Label
                          prop.onClick (fun e -> scrollToElement e visualization.ClassName)
                        ] |> Html.h2
                    brandLink
                ]
            ]

    Html.div
        [ prop.className [ true, "visualization container" ; embedded, "embeded" ]
          prop.children (
              visualizations
              |> List.map (fun viz ->
                  Html.section
                    [ prop.className [ true, viz.ClassName; true, "visualization-chart" ]
                      prop.id viz.ClassName
                      prop.children
                        [ renderChartTitle viz
                          state |> viz.Renderer
                        ]
                    ]
            ) )
        ]
