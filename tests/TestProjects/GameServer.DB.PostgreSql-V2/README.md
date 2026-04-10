# GameServer.DB PostgreSQL Database Project

This project defines the V2 backend schema for PostgreSQL using `pgPacTool`.

All V2 PostgreSQL tables in this project are deployed into the `core` schema rather than `public`.

Project layout:

- `core/Tables/*.sql` defines all V2 tables and indexes in that schema

## Tooling

```powershell
dotnet tool restore
```

The project uses the `MSBuild.Sdk.PostgreSql` SDK package for project structure and build integration, and the local `pgpac` CLI for deployment operations.

## Compile

```powershell
dotnet build .\src\GameServer.DB.PostgreSql\GameServer.DB.PostgreSql.csproj -c Release
```

## Deploy

```powershell
.\scripts\Deploy-V2PostgresDatabase.ps1 -TargetConnectionString "Host=localhost;Database=gameserver-v2;Username=postgres;Password=postgres"
```

The application does not create the V2 PostgreSQL schema with EF migrations. Deploy this project first with `pgpac publish` before running the API against PostgreSQL. The application startup check expects V2 PostgreSQL tables such as `core."GameTypes"` to exist after deployment.

## Database Diagram

The diagram below is aligned with the current V2 entities in `src/GameServer.Docker/Data/V2/Entities.cs` and the SQL files in this project.

```mermaid
erDiagram
    GameTypes {
        int Id PK
        string Key UK
        string DisplayName
        string Description
        string Type
        string ThumbnailUrl
        string DocumentationUrl
        bool IsActive
        int CurrentRevisionId FK
        datetime CreatedAt
        datetime UpdatedAt
    }

    GameTypeRevisions {
        int Id PK
        int GameTypeId FK
        string VersionTag
        string ImageReference
        string ImageDigest
        bool EnableTTY
        string Notes
        bool IsPublished
        datetime CreatedAt
    }

    GameTypePorts {
        int Id PK
        int GameTypeRevisionId FK
        int ContainerPort
        string Protocol
        bool AdvertisedPort
        string Description
        int DisplayOrder
    }

    GameTypeVolumes {
        int Id PK
        int GameTypeRevisionId FK
        string Source
        string Description
        int DisplayOrder
        string Usage
    }

    GameTypeSettingDefinitions {
        int Id PK
        int GameTypeRevisionId FK
        string SettingKey
        string DefaultValue
        string Description
        int DisplayOrder
    }

    GameTypeSettingMetadata {
        int Id PK
        int GameTypeSettingDefinitionId FK
        string DataType
        string Category
        bool IsRequired
        bool CannotBeEmpty
        string Placeholder
        string ValidationPattern
        string ValidationMessage
        bool AutoAllocatePort
        bool ValidateRelatedPortsAvailability
        string AllowedValuesJson
        string ValueMappingsJson
    }

    GameTypeSettingPortMappings {
        int Id PK
        int GameTypeSettingMetadataId FK
        int MappingRole
        int RelationType
        int TargetContainerPort
        string TargetProtocol
        int CalculationValue
        bool IsRequired
        int DisplayOrder
    }

    GameTypeWebHosts {
        int Id PK
        int GameTypeRevisionId FK
        string Name
        string Description
        string PathSegment
        int ContainerPort
        string ContainerPortVariable
        string EnabledWhen
        int DisplayOrder
    }

    GameServers {
        int Id PK
        string ServerId UK
        string Name
        string Description
        int GameTypeRevisionId FK
        string ServiceName
        string Status
        datetime CreatedAt
        datetime UpdatedAt
        datetime LastDeployedAt
        datetime LastSeenAt
        bool IsDeleted
    }

    GameServerSettings {
        int Id PK
        int GameServerId FK
        string SettingKey
        string Value
    }

    GameTypes ||--o{ GameTypeRevisions : has
    GameTypeRevisions ||--o{ GameTypePorts : defines
    GameTypeRevisions ||--o{ GameTypeVolumes : defines
    GameTypeRevisions ||--o{ GameTypeSettingDefinitions : defines
    GameTypeRevisions ||--o{ GameTypeWebHosts : defines
    GameTypeRevisions ||--o{ GameServers : selected_by
    GameTypeSettingDefinitions ||--o| GameTypeSettingMetadata : describes
    GameTypeSettingMetadata ||--o{ GameTypeSettingPortMappings : maps
    GameServers ||--o{ GameServerSettings : configures
```

## Deployment Flow Diagram

This view focuses on how authored V2 data flows into deployment-time state.

```mermaid
flowchart TD
    GT[GameType<br/>Catalog identity] --> REV[GameTypeRevision<br/>Deployable image + template]

    REV --> PORTS[GameTypePorts]
    REV --> VOLUMES[GameTypeVolumes]
    REV --> SETTINGS[GameTypeSettingDefinitions]
    REV --> WEBHOSTS[GameTypeWebHosts]

    SETTINGS --> META[GameTypeSettingMetadata]
    META --> PMAPS[GameTypeSettingPortMappings]

    REV --> GS[GameServer<br/>References GameTypeRevisionId]
    GS --> GSS[GameServerSettings<br/>Server overrides]

    PORTS -. derive .-> RESOLVEDPORTS[Resolved published ports<br/>Not stored]
    VOLUMES -. derive .-> RESOLVEDVOLUMES[Resolved volumes<br/>Not stored]
    WEBHOSTS -. resolve with settings .-> RESOLVEDWEB[Resolved web hosts<br/>Not stored]
    GSS -. influence .-> RESOLVEDPORTS
    GSS -. influence .-> RESOLVEDWEB
    PMAPS -. calculate .-> RESOLVEDPORTS

    RESOLVEDPORTS --> DEPLOY[Primary Service deployment<br/>Swarm service configuration]
    RESOLVEDVOLUMES --> DEPLOY
    RESOLVEDWEB --> DEPLOY

    class GT,REV,PORTS,VOLUMES,SETTINGS,WEBHOSTS,META,PMAPS,GS,GSS persisted
    class RESOLVEDPORTS,RESOLVEDVOLUMES,RESOLVEDWEB derived
    class DEPLOY deployment

    classDef persisted fill:#dbeafe,stroke:#1d4ed8,stroke-width:1px,color:#111827
    classDef derived fill:#dcfce7,stroke:#16a34a,stroke-width:1px,color:#111827
    classDef deployment fill:#fef3c7,stroke:#d97706,stroke-width:1px,color:#111827
```

Legend:
- Blue = persisted V2 tables
- Green = deployment-time derived state
- Amber = deployment output / orchestration target

### Notes

- `GameTypes` owns catalog identity; `GameTypeRevisions` owns deployable image details.
- `GameServers` references only `GameTypeRevisionId` and derives game type/image information through that revision.
- Port and volume instances are intentionally not stored per server in V2.
- Port mapping rows store a direct primary mapping plus optional related mapping rules using `CalculationValue`.
- Port mapping descriptions are shown from the linked `GameTypePorts.Description` and are not stored on the mapping rows.
- The more detailed reference copy of this diagram lives in `docs/reference/V2-Database-Diagram.md`.

