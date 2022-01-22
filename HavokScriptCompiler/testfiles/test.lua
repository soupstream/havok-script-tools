
function factorial(n)
    if (n == 0) then
        return 1
    else
        return n * factorial(n - 1)
    end
end

function dofunction(fn, arg)
    return fn(arg)
end

print(dofunction(factorial, 5))
