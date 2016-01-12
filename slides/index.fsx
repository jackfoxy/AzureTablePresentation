(**
- title : Azure Table Storage at Tachyus
- description : Azure Table Storage at Tachyus
- author : Jack Fox
- theme : night
- transition : default

***

### Who Am I?

Jack Fox

- Software Engineer at Tachyus, San Mateo
- Microsoft .Net MVP
- Twitter [@foxyjackfox](https://twitter.com/foxyjackfox) 
- https://github.com/jackfoxy

![Tachyus](images/tachyus_reverse_block_hires.png)

***

### Obligatory Humor 

> When I show my boss that I've fixed a bug**
  
![When I show my boss that I've fixed a bug](http://www.topito.com/wp-content/uploads/2013/01/code-07.gif)

***

### Azure Table Storage 

- Up to TBs of structured data
- Denormalized datasets that don't require joins or foreign keys 
- Fast querying using a clustered index (can use partial keys)
- Access data using the OData protocol and LINQ queries

***

### Sparse Key-Value Data

![Tachyus](images/SparseData2.png)

*)
(*** hide ***)
#I "../../Entoleon/packages"
#r "FsPickler/lib/net45/FsPickler.dll"
#r "FSharp.Azure.Storage/lib/net45/FSharp.Azure.Storage.dll"
#r "WindowsAzure.Storage/lib/net40/Microsoft.WindowsAzure.Storage.dll"

(**

***

### Set-up Overhead

*)
open FSharp.Azure.Storage.Table
open Microsoft.WindowsAzure.Storage
open Nessos.FsPickler
open System

let account = CloudStorageAccount.Parse "azure connection string" 
let tableClient  = account.CreateCloudTableClient()

(**

***

### Inserting Records

*)
type Schedule =
    { 
    [<PartitionKey>] ScheduleHash : string
    [<RowKey>] RowKey : string
    IsActive : bool
    Schedule : string
    CreateTime : DateTime
    }

// make a record
let schedule = {ScheduleHash = "foo"; RowKey = "bar"; IsActive = true; 
                Schedule = "something"; CreateTime = DateTime.UtcNow}

// helper function
let inScheduleTable schedule = 
    inTable tableClient "Schedules" schedule

// insert to table storage
let result = 
    schedule |> Insert |> inScheduleTable


(**

***

### Generic Query by Partion and Row Keys

*)
let queryByKey tableName (partitionKey : string) (rowKey : string)  azureConnString 
    (entityQuery : EntityQuery<'T>) =

    entityQuery
    |> Query.where <@ fun _ s -> s.PartitionKey = partitionKey && s.RowKey = rowKey @> 
    |> fromTable tableClient tableName

    // can select by partial key, and return entire sequence (enumerable)
    |> Seq.head    // to select only one record

(** Quick access *)

(**

***

### Specific Table Query by Non-Key Column

*)

let queryAllActive tableName azureConnString (entityQuery : EntityQuery<Schedule>) =

    entityQuery
    |> Query.where <@ fun g _ -> g.IsActive @>
    |> fromTable tableClient tableName

(** Maybe not so quick access, but gets the job done *)

(**

***

### So Far, So Good

> But we haven't dealt with any sparse data...yet.

> Now for the heavy lifting.

![Now for the heavy lifting](images/Heavy_Lifting.jpg)

***

### F# Option Type ... Sparse Data Handled!

*)
type Log =
    { 
    [<PartitionKey>] Caller : string
    [<RowKey>] RunTime : string
    LocalTime : DateTime option
    Severity : string
    Message : string
    Assemblies : string
    ScheduleHash : string option
    ScheduleExecutionTime : DateTime option
    QueryHash : string option
    StartTime : DateTime option
    Exception : byte []
    }
(**

***

### F# Option Types

*)
let thisIsSomeString = Some "a string"

let thisIsNoString = None

let workWithOptionalString (mightBeString : string option) =
    match mightBeString with
    | Some x ->
        // do something with x
        let myString = x
        ()
    | None ->
        ()
(**

***

### Passing On Strongly Typed Exception

*)

let binary = FsPickler.CreateBinarySerializer()

let serializedException =
    try
        let blah = thisIsSomeString
        // ...and pretend we have something that could throw here
        (Array.create 1 (new Byte())
    
    with e -> 
        binary.Pickle x
    
// ...and on the other side of the planet we deserialize it

let deserializeException (exc : byte []) =
    binary.UnPickle<Exception> exc

(**

***

### Serialize All Data, Strongly Typed

* serialize data column-wise in strongly typed lists
* save serializations to blob storage
* save a little bit of meta data to define rows
* deserialize on other side of planet
* reconstitute row-wise

Done!

*)