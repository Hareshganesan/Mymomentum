namespace Momentum.Content
{
    public sealed class CardDef
    {
        public string Id { get; init; } = string.Empty;
        public string Subject { get; init; } = string.Empty;
        public string Question { get; init; } = string.Empty;
        public string Answer { get; init; } = string.Empty;
        public bool IsCustom { get; init; }
    }

    /// <summary>
    /// High-yield Core CS questions asked in campus and SDE-1 interviews.
    /// Answers are written the way you'd say them out loud: definition first, then the one detail that shows depth.
    /// </summary>
    public static class CsCatalog
    {
        public static readonly IReadOnlyList<(string Key, string Name)> Subjects = new List<(string, string)>
        {
            ("OS", "Operating Systems"),
            ("DBMS", "DBMS"),
            ("SQL", "SQL"),
            ("CN", "Computer Networks"),
            ("OOP", "OOP & Design"),
            ("SD", "System Design"),
        };

        public static string SubjectName(string key) => Subjects.FirstOrDefault(s => s.Key == key).Name ?? key;

        public static IReadOnlyList<CardDef> Cards { get; }

        static CsCatalog()
        {
            var list = new List<CardDef>();
            string subject = "OS";
            int n = 0;
            void C(string q, string a) => list.Add(new CardDef { Id = $"{subject.ToLowerInvariant()}-{++n:D2}", Subject = subject, Question = q, Answer = a });

            subject = "OS"; n = 0;
            C("Process vs Thread?",
              "A process is an independent program in execution with its own address space, file descriptors and resources. A thread is a unit of execution inside a process; threads share the heap, code and open files but each has its own stack, registers and program counter.\nThreads are cheaper to create and switch, but a bug in one thread can corrupt the whole process.");
            C("What happens during a context switch?",
              "The kernel saves the running task's CPU state (registers, PC, stack pointer) into its PCB/TCB, picks the next task via the scheduler, and restores that task's state. Switching between processes also changes the address space (page table base register), which flushes or tags the TLB — that's why process switches cost more than thread switches.");
            C("What is a deadlock? Name the four Coffman conditions.",
              "A set of processes each waiting for a resource held by another, so none can proceed.\nAll four must hold: Mutual exclusion, Hold and wait, No preemption, Circular wait. Breaking any one prevents deadlock — e.g. acquiring locks in a global order breaks circular wait.");
            C("Deadlock prevention vs avoidance vs detection?",
              "Prevention: design so one Coffman condition can never hold (lock ordering, request all resources at once).\nAvoidance: decide at runtime whether granting a request keeps the system in a safe state — Banker's algorithm.\nDetection & recovery: let deadlocks happen, detect cycles in the wait-for graph, then kill or roll back a victim.");
            C("Mutex vs Semaphore vs Spinlock?",
              "Mutex: a lock with ownership — only the thread that locked it can unlock it; protects a critical section.\nSemaphore: an integer counter with wait/signal; a counting semaphore allows N concurrent holders and is used for signalling between threads.\nSpinlock: busy-waits instead of sleeping; good only for very short critical sections on multi-core systems.");
            C("What is a race condition and how do you prevent it?",
              "When the result depends on the interleaving of threads accessing shared data, e.g. two threads doing count++ (read-modify-write) and losing an update.\nPrevent with mutual exclusion (locks), atomic operations (compare-and-swap), or by avoiding shared mutable state.");
            C("Paging vs Segmentation?",
              "Paging splits memory into fixed-size pages/frames; no external fragmentation but some internal fragmentation, and it's invisible to the programmer.\nSegmentation splits by logical units (code, stack, heap) of variable size; matches program structure but suffers external fragmentation. Modern systems use paging (x86-64 keeps segmentation only vestigially).");
            C("What is virtual memory and why is it useful?",
              "Each process gets its own large, contiguous logical address space mapped to physical frames by page tables. Pages can live on disk and be loaded on demand.\nBenefits: isolation between processes, running programs larger than RAM, cheap fork via copy-on-write, and shared libraries mapped once.");
            C("What is a page fault and what is thrashing?",
              "A page fault occurs when a process touches a page not currently in RAM; the OS loads it from disk and updates the page table.\nThrashing is when the working sets of running processes exceed physical memory, so the system spends more time swapping pages than executing. Fix by reducing multiprogramming or adding memory.");
            C("Explain the TLB.",
              "The Translation Lookaside Buffer is a small, fast cache of recent virtual→physical page translations inside the MMU. A TLB hit avoids walking the multi-level page table in memory; a miss costs several memory accesses. Context switches may flush it unless entries are tagged with an address-space ID.");
            C("Page replacement algorithms — FIFO, LRU, Optimal?",
              "FIFO evicts the oldest page (simple, suffers Belady's anomaly). Optimal evicts the page used farthest in the future (theoretical benchmark). LRU evicts the least recently used page — a good approximation of Optimal, usually implemented approximately with reference bits (Clock algorithm).");
            C("CPU scheduling: FCFS, SJF, Round Robin, Priority?",
              "FCFS: run in arrival order; convoy effect.\nSJF/SRTF: shortest job first; optimal average waiting time but needs burst prediction and can starve long jobs.\nRound Robin: fixed time quantum; good response time, performance depends on quantum size.\nPriority: highest priority first; starvation fixed by aging.");
            C("What is a zombie process vs an orphan process?",
              "Zombie: a child that has exited but whose parent hasn't called wait(), so its PCB entry remains to hold the exit status.\nOrphan: a child whose parent died first; it's re-parented to init/systemd, which reaps it.");
            C("User mode vs kernel mode; what is a system call?",
              "User mode restricts privileged instructions and direct hardware access; kernel mode has full access. A system call is the controlled entry point from user code into the kernel (via a trap instruction) for services like file I/O, process creation or memory allocation.");
            C("Internal vs external fragmentation?",
              "Internal: allocated block is larger than requested, so space inside it is wasted (fixed-size pages).\nExternal: enough total free memory exists but not contiguously (variable-size segments). Fixed with compaction or paging.");

            subject = "DBMS"; n = 0;
            C("Explain ACID properties.",
              "Atomicity: a transaction happens fully or not at all (undo log).\nConsistency: it moves the database from one valid state to another, respecting constraints.\nIsolation: concurrent transactions behave as if executed in some serial order.\nDurability: once committed, changes survive crashes (write-ahead log flushed to disk).");
            C("What are the transaction isolation levels and their anomalies?",
              "Read Uncommitted → dirty reads possible.\nRead Committed → no dirty reads, but non-repeatable reads.\nRepeatable Read → same row reads are stable, phantoms possible (MySQL InnoDB prevents most with gap locks).\nSerializable → no anomalies, lowest concurrency.");
            C("Normalization: 1NF, 2NF, 3NF, BCNF?",
              "1NF: atomic values, no repeating groups.\n2NF: 1NF + no partial dependency of a non-key attribute on part of a composite key.\n3NF: 2NF + no transitive dependency (non-key → non-key).\nBCNF: for every functional dependency X → Y, X is a super key.\nGoal: remove update/insert/delete anomalies.");
            C("When would you denormalize?",
              "When read performance matters more than write simplicity — e.g. analytics or hot read paths where joins are too expensive. You duplicate data (counters, cached names) and accept the cost of keeping copies consistent.");
            C("How does a B+ tree index work, and why do databases use it?",
              "A balanced, high-fan-out tree where internal nodes hold only keys and all records/pointers live in linked leaf nodes. Height stays ~3–4 even for millions of rows, so lookups take few disk reads, and the linked leaves make range scans fast.");
            C("Clustered vs non-clustered index?",
              "Clustered: the table rows are physically stored in index order; only one per table (often the primary key).\nNon-clustered: a separate structure with keys and pointers (or the primary key) to rows; many allowed, but lookups may need an extra hop to fetch the row.");
            C("Primary key vs unique key vs foreign key?",
              "Primary key: uniquely identifies a row, not null, one per table.\nUnique key: enforces uniqueness, can usually allow a NULL, many per table.\nForeign key: references a primary/unique key in another table to enforce referential integrity.");
            C("What is a deadlock in databases and how is it handled?",
              "Two transactions each hold a lock the other needs. DB engines detect cycles in the wait-for graph (or use timeouts) and abort one victim transaction, which the application should retry. Accessing tables in a consistent order reduces it.");
            C("What is MVCC?",
              "Multi-Version Concurrency Control keeps multiple versions of a row so readers see a consistent snapshot without blocking writers, and writers don't block readers. Used by PostgreSQL and InnoDB; old versions are cleaned up later (vacuum/purge).");
            C("What is a write-ahead log (WAL)?",
              "Changes are first appended to a sequential log and flushed before the data pages are modified. On crash, the DB replays (redo) committed changes and undoes uncommitted ones. Gives durability and atomicity with fast sequential writes.");
            C("SQL vs NoSQL — when to use which?",
              "SQL: structured schema, joins, strong consistency and transactions — payments, inventory, most business data.\nNoSQL (document, key-value, wide-column, graph): flexible schema and easy horizontal scaling — high write throughput, large unstructured or rapidly changing data. Many systems use both.");
            C("What is the CAP theorem?",
              "In a distributed system, during a network Partition you must choose between Consistency (every read sees the latest write) and Availability (every request gets a response). Without partitions you can have both. Example: banking leans CP; a social feed leans AP.");
            C("What is sharding vs replication?",
              "Replication copies the same data to multiple nodes for availability and read scaling (leader–follower).\nSharding splits data across nodes by a key (range or hash) so each node holds a subset — scales writes and storage, but cross-shard queries and rebalancing get harder.");
            C("What are database views and stored procedures?",
              "A view is a saved query presented as a virtual table — used for abstraction and security. A materialized view stores the result physically and must be refreshed.\nA stored procedure is precompiled SQL logic stored in the DB, reducing round trips but moving business logic into the database.");

            subject = "SQL"; n = 0;
            C("Types of JOINs?",
              "INNER: rows matching in both tables.\nLEFT/RIGHT OUTER: all rows from one side plus matches (NULLs otherwise).\nFULL OUTER: all rows from both.\nCROSS: Cartesian product.\nSELF JOIN: a table joined with itself, e.g. employee → manager.");
            C("WHERE vs HAVING?",
              "WHERE filters rows before grouping and can't use aggregates. HAVING filters groups after GROUP BY and can use aggregates, e.g. HAVING COUNT(*) > 5.");
            C("Find the second highest salary.",
              "SELECT MAX(salary) FROM Employee WHERE salary < (SELECT MAX(salary) FROM Employee);\nOr generally the N-th: use DENSE_RANK() OVER (ORDER BY salary DESC) and filter rank = N.");
            C("DELETE vs TRUNCATE vs DROP?",
              "DELETE removes selected rows, is logged per row, fires triggers and can be rolled back.\nTRUNCATE removes all rows quickly by deallocating pages, resets identity, minimal logging.\nDROP removes the table structure itself.");
            C("RANK vs DENSE_RANK vs ROW_NUMBER?",
              "ROW_NUMBER: unique sequential numbers, ties broken arbitrarily (1,2,3,4).\nRANK: ties share a rank and leave gaps (1,2,2,4).\nDENSE_RANK: ties share a rank without gaps (1,2,2,3).");
            C("Find duplicate emails in a table.",
              "SELECT email FROM Person GROUP BY email HAVING COUNT(*) > 1;\nTo delete duplicates keeping the lowest id: DELETE p1 FROM Person p1 JOIN Person p2 ON p1.email = p2.email AND p1.id > p2.id;");
            C("What is a correlated subquery?",
              "A subquery that references columns from the outer query, so it runs once per outer row. E.g. employees earning more than their department average:\nSELECT * FROM Emp e WHERE salary > (SELECT AVG(salary) FROM Emp WHERE dept = e.dept);");
            C("UNION vs UNION ALL?",
              "UNION combines result sets and removes duplicates (requires a sort/hash step). UNION ALL keeps duplicates and is faster. Both need the same number and compatible types of columns.");

            subject = "CN"; n = 0;
            C("What happens when you type a URL into the browser?",
              "1) DNS resolution (browser cache → OS → resolver → root → TLD → authoritative).\n2) TCP 3-way handshake with the server IP (or QUIC for HTTP/3).\n3) TLS handshake: certificate verification and key exchange.\n4) HTTP request sent; server/CDN/load balancer responds.\n5) Browser parses HTML, fetches CSS/JS/images, builds the DOM and renders.");
            C("OSI model vs TCP/IP model?",
              "OSI has 7 layers: Physical, Data Link, Network, Transport, Session, Presentation, Application.\nTCP/IP has 4: Link, Internet (IP), Transport (TCP/UDP), Application (HTTP, DNS…). OSI is a teaching model; TCP/IP is what the internet runs on.");
            C("TCP vs UDP?",
              "TCP: connection-oriented, reliable, ordered delivery, flow and congestion control — HTTP/1.1-2, email, file transfer.\nUDP: connectionless, no delivery guarantee, low overhead and latency — DNS, video calls, gaming, QUIC builds reliability on top of it.");
            C("Explain the TCP 3-way handshake and 4-way teardown.",
              "Handshake: client SYN (seq x) → server SYN-ACK (seq y, ack x+1) → client ACK (ack y+1). Both sides agree on initial sequence numbers.\nTeardown: FIN → ACK, then the other side's FIN → ACK; the side that closed first waits in TIME_WAIT to catch late packets.");
            C("How does DNS work?",
              "DNS maps names to IP addresses. A recursive resolver asks a root server for the TLD server, the TLD server for the domain's authoritative server, and the authoritative server for the record (A, AAAA, CNAME, MX). Results are cached according to TTL at every level.");
            C("HTTP vs HTTPS; how does TLS work at a high level?",
              "HTTPS is HTTP over TLS. In the handshake the server presents a certificate signed by a trusted CA, the client verifies it, and both derive a shared session key (e.g. via ECDHE). Data is then encrypted symmetrically, providing confidentiality, integrity and server authentication.");
            C("HTTP/1.1 vs HTTP/2 vs HTTP/3?",
              "HTTP/1.1: text-based, one request at a time per connection (head-of-line blocking), keep-alive.\nHTTP/2: binary framing, multiplexed streams over one TCP connection, header compression.\nHTTP/3: runs over QUIC (UDP), removing TCP-level head-of-line blocking and enabling faster connection setup.");
            C("Common HTTP methods and status codes?",
              "GET (read, idempotent), POST (create), PUT (replace, idempotent), PATCH (partial update), DELETE (idempotent).\n2xx success (200 OK, 201 Created, 204 No Content); 3xx redirect (301, 302, 304); 4xx client error (400, 401, 403, 404, 429); 5xx server error (500, 502, 503).");
            C("What is a subnet mask / CIDR?",
              "CIDR notation like 192.168.1.0/24 means the first 24 bits are the network part and the remaining 8 bits address hosts (256 addresses, 254 usable). Subnetting splits a network into smaller ones for routing efficiency and isolation.");
            C("Router vs Switch vs Hub?",
              "Hub: layer 1, repeats every frame to all ports.\nSwitch: layer 2, forwards frames by MAC address using a MAC table.\nRouter: layer 3, forwards packets between networks using IP routing tables.");
            C("What is ARP?",
              "Address Resolution Protocol maps an IP address to a MAC address on a local network: a host broadcasts 'who has IP X?', and the owner replies with its MAC, which is cached in the ARP table.");
            C("What is NAT?",
              "Network Address Translation lets many devices with private IPs share one public IP. The router rewrites source IP/port on outgoing packets and keeps a translation table to route replies back. It conserves IPv4 addresses.");
            C("Cookies vs Sessions vs JWT?",
              "Cookie: small data stored in the browser and sent with every request to that domain.\nSession: state stored on the server, referenced by a session ID cookie.\nJWT: a signed, self-contained token carrying claims; the server verifies the signature without a lookup, but revocation is harder.");
            C("What is a CDN?",
              "A Content Delivery Network caches static (and sometimes dynamic) content on edge servers close to users, cutting latency and offloading the origin. Requests are routed to the nearest edge via DNS or anycast.");

            subject = "OOP"; n = 0;
            C("Four pillars of OOP?",
              "Encapsulation: bundle data with methods and hide internal state behind an interface.\nAbstraction: expose what an object does, not how.\nInheritance: derive a class from another to reuse and specialise behaviour.\nPolymorphism: one interface, many implementations — chosen at compile time (overloading) or runtime (overriding).");
            C("Overloading vs Overriding?",
              "Overloading: same method name, different parameters, resolved at compile time.\nOverriding: a subclass provides its own implementation of a base virtual method with the same signature, resolved at runtime through dynamic dispatch (vtable).");
            C("Abstract class vs Interface?",
              "Abstract class: can hold state, constructors and partially implemented methods; a class extends only one.\nInterface: a contract of behaviour (modern languages allow default methods but no instance state); a class can implement many. Use interfaces for capabilities, abstract classes for shared base behaviour.");
            C("Explain SOLID.",
              "S – Single Responsibility: one reason to change.\nO – Open/Closed: open for extension, closed for modification.\nL – Liskov Substitution: subtypes must be usable wherever the base type is expected.\nI – Interface Segregation: many small interfaces over one fat one.\nD – Dependency Inversion: depend on abstractions, not concrete classes.");
            C("Composition vs Inheritance?",
              "Inheritance is an 'is-a' relationship and couples the child to the parent's implementation. Composition is 'has-a': build behaviour from smaller objects. Prefer composition — it's more flexible and avoids fragile base classes.");
            C("What is the Singleton pattern? Downsides?",
              "Ensures a class has a single instance with a global access point (private constructor + static instance, thread-safe via lazy init or double-checked locking).\nDownsides: hidden global state, harder testing, and hidden coupling — dependency injection is usually better.");
            C("Factory vs Abstract Factory vs Builder?",
              "Factory method: a method decides which concrete class to instantiate.\nAbstract factory: creates families of related objects without naming concrete classes.\nBuilder: constructs a complex object step by step, avoiding huge constructors.");
            C("Explain the Observer and Strategy patterns.",
              "Observer: subjects notify registered observers of state changes (event systems, pub/sub).\nStrategy: encapsulate interchangeable algorithms behind an interface and pick one at runtime (e.g. payment methods, sorting comparators).");
            C("Shallow copy vs Deep copy?",
              "Shallow copy duplicates the top-level object but shares references to nested objects. Deep copy recursively duplicates everything, so the copy is fully independent.");
            C("What is a virtual function and a vtable (C++)?",
              "A virtual function can be overridden and is dispatched at runtime. Each class with virtual functions has a vtable of function pointers, and each object holds a hidden vptr to its class's vtable; calls go through it. Base classes need virtual destructors when deleting through a base pointer.");
            C("Stack vs Heap memory?",
              "Stack: automatic, LIFO, per thread, stores locals and call frames; very fast, limited size.\nHeap: dynamic allocation with manual or GC-managed lifetime; larger but slower and prone to fragmentation and leaks.");
            C("Design a Parking Lot (LLD) — what classes would you use?",
              "ParkingLot (floors, entry/exit gates), ParkingFloor (spots), ParkingSpot (type: compact/large/bike, isFree), Vehicle (subclasses Car/Bike/Truck), Ticket (spot, vehicle, entry time), PaymentStrategy (cash/card/UPI), and a SpotAllocationStrategy (nearest first). Talk through extensibility: new vehicle types, pricing rules, concurrency on spot assignment.");

            subject = "SD"; n = 0;
            C("Horizontal vs vertical scaling?",
              "Vertical: a bigger machine (more CPU/RAM) — simple but has a ceiling and a single point of failure.\nHorizontal: more machines behind a load balancer — near-unlimited scale and fault tolerance, but needs stateless services and distributed data.");
            C("What does a load balancer do? Common algorithms?",
              "Distributes traffic across servers for scale and availability, with health checks to drop bad instances.\nAlgorithms: round robin, weighted round robin, least connections, IP hash (sticky). Works at L4 (TCP) or L7 (HTTP, can route by path/header).");
            C("Caching strategies and eviction policies?",
              "Cache-aside (app reads cache, falls back to DB and populates), write-through (write cache and DB together), write-back (write cache, flush later), write-around.\nEviction: LRU, LFU, FIFO, TTL. Watch for stale data, cache stampedes and choosing what to invalidate.");
            C("What is consistent hashing?",
              "Nodes and keys are hashed onto a ring; a key goes to the next node clockwise. Adding or removing a node moves only ~1/N of the keys. Virtual nodes smooth out the distribution. Used in distributed caches and databases.");
            C("Message queues — why use them?",
              "Kafka/RabbitMQ/SQS decouple producers from consumers: absorb traffic spikes, enable async processing and retries, and let multiple services consume the same events. Trade-offs: eventual consistency, ordering and exactly-once semantics need care.");
            C("Design a URL shortener — key points.",
              "API: POST long URL → short code; GET code → 301/302 redirect.\nCode generation: base62 of an auto-increment ID or a hash with collision checks.\nStorage: key-value store code → URL; heavy reads so cache hot codes. Add analytics asynchronously, TTL/expiry, and rate limiting.");
            C("How would you design a rate limiter?",
              "Algorithms: token bucket (allows bursts), leaky bucket (smooth output), fixed window counter, sliding window log/counter. Store counters in Redis keyed by user/IP with TTL; return HTTP 429 with Retry-After. In a distributed setup use atomic operations or Lua scripts.");
            C("Monolith vs Microservices?",
              "Monolith: one deployable — simple to build, test and deploy early on, but scales and evolves as a single unit.\nMicroservices: independently deployable services owned by teams; scale and ship independently, at the cost of network calls, distributed data and operational complexity. Start monolith, split along clear domain boundaries.");

            Cards = list;
        }
    }
}
