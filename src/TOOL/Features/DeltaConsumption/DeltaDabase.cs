using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

public class DeltaContext : DbContext
{
    public DbSet<Rule
}


public class Rule
{
    public required string Ns { get; set; }
    public required string item_id { get; set; }
    public required int version { get; set; }
    public required string title { get; set; }
    public required string content { get; set; }
    public required string labelsJson { get; set; }
    public required int isActive { get; set; }
    public required int policyVersion { get; set; }
    public required DateTime OccuredAt { get; set; }
    public required DateTime EmittedAt { get; set; }
}

public class RuleHistory
{
    public required string Ns { get; set; }
    public required int itemId { get; set; }
    public required int version { get; set; }
    public required string title { get; set; }
    public required string content { get; set; }
    public required string labelsJson { get; set; }
    public required int isActive { get; set; }
    public required int policyVersion { get; set; }
    public required DateTime OccuredAt { get; set; }
    public required DateTime EmittedAt { get; set; }
}

public class SourceBindings
{
    public required string Ns { get; set; }
    public required int itemId { get; set; }
    public required int version { get; set; }



}

CREATE TABLE IF NOT EXISTS source_bindings (
                ns TEXT NOT NULL,
                item_id TEXT NOT NULL,
                version INTEGER NOT NULL,
                repo TEXT NOT NULL,
                ref TEXT NOT NULL,
                path TEXT NOT NULL,
                blob_sha TEXT NOT NULL,
                PRIMARY KEY (ns, item_id, version)
            );
